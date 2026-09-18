using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// PROJEKTMODUS der Kostenverwaltung (Etappe KD6a, Konzept Kostendialoge
    /// § 3.2/§ 5 — der in KD3 auf KD6 verschobene dritte Kontext, Nutzerabnahme
    /// 26.08.2026): Lesen und Pflegen der Tab_ProjektWerte-Positionen einer
    /// Komponente und Kategorie in DERSELBEN Rasterdarstellung wie die
    /// Stammvorlagen (<see cref="ucVorlagenZeile"/>).
    ///
    /// <para><b>Keine neue Wahrheit:</b> Gelesen wird über die Bestandsspalten
    /// samt <see cref="KostenPositionCtrl.LiesZusatz"/>; geschrieben wird
    /// ausschließlich über die Bestands-Schreibwege
    /// (<see cref="KostenPositionCtrl.SetzeBetragMitZusatz"/>,
    /// <see cref="KostenVorlagenUebernahmeCtrl.StammIdSicher"/> — dieselben Wege
    /// wie Übernahme-Mechanik und alter Kosteneditor). Der ANZEIGEbetrag
    /// satzbasierter Positionen kommt aus <see cref="BetriebskostenCtrl.Betrag"/>
    /// — dem einen Rechenweg, den auch der Rechenkern nutzt.</para>
    /// </summary>
    internal static class KostenProjektPositionenCtrl
    {
        /// <summary>Eine Projektzeile in Vorlagen-Rasterform; <c>Id</c> ist
        /// <c>Tab_ProjektWerte.ID</c>, <c>BetragNetto</c> der BERECHNETE Betrag.</summary>
        internal sealed class Zeile
        {
            public KostenVorlagenPosition Raster = new KostenVorlagenPosition();
            public double Eingegeben;
            public double? Menge;
            public double Best;
            public double Worst;
            public double BestNutzung;
            public double WorstNutzung;
            public int StartJahr;

            /// <summary>ANWENDERBEFUND W5‑B‑7 (08.09.2026): die BEZUGSGRÖSSE, aus der
            /// <c>Raster.BetragNetto</c> entstanden ist — bei „3 % der Investition" also
            /// die 5.660,00 €. Reine Auskunft für den Werkzeugtipp des Dialogs; sie wird
            /// NICHT nach <c>Tab_ProjektWerte.Menge</c> zurückgeschrieben (dort ist die
            /// Menge Ausweisgröße des Laufs, und für „% der Investition" in Kategorie 1
            /// schreibt <c>WirtschaftlichkeitCtrl.MengeAusweisen</c> bewusst nichts).</summary>
            public double? Basis;

            /// <summary>ANWENDERBEFUND 10.09.2026 (H4c): WARUM es keine
            /// <see cref="Basis"/> gibt — einer der Steuerwerte
            /// <c>WirtschaftlichkeitCtrl.BASISGRUND_*</c>, leer bei absoluten Arten und
            /// überall dort, wo eine Basis steht. Der Anwender sah bis dahin nur die 0,
            /// die der Anwenderentscheid I-2 daraus macht; den Satz dazu baut die
            /// Oberfläche (Drei-Schichten-Regel).</summary>
            public string BasisGrund = "";

            /// <summary>ANWENDERBEFUND 14.09.2026: WIE die <see cref="Basis"/>
            /// entstanden ist, wenn sie GERECHNET und nicht am Gerät abgelesen wurde
            /// — „0,7 kW/m² × 2,50 m² × 10 Module = 17,50 kW". Leer, wo die Größe
            /// unmittelbar in einer Gerätemaske steht. Den Satz baut der Kern, weil
            /// er die Formel kennt; die Oberfläche zeigt ihn nur.</summary>
            public string BasisHerleitung = "";

            /// <summary>U28: Die RUNDE der Investitionskaskade, in der der Betrag
            /// entstanden ist (1…3); 0 auf der Betriebsseite, die keine Kaskade
            /// kennt. Sie kommt aus <see cref="InvestKaskade.Zeile.Runde"/>.</summary>
            public int Runde;

            /// <summary>U28: WOHER die <see cref="Basis"/> stammt — Steuerwert
            /// <c>KostenHerleitung.HERKUNFT_*</c>. Auf der Investitionsseite schreibt
            /// die Kaskade ihn mit, auf der Betriebsseite folgt er der Bemessungsart
            /// („% der Investition" bemisst sich an der Investitionssumme, alles
            /// andere an einer Menge des Laufs).</summary>
            public string BasisHerkunft = "";

            /// <summary>Projekt und Kategorie der Zeile — damit
            /// <see cref="Speichern"/> den wirksamen Betrag über denselben Rechenweg
            /// nachziehen kann wie <see cref="Lies(int,int,int,int)"/>.</summary>
            public int ProjektId;
            public int KategorieId;
        }

        // ------------------------------------------------------------- Lesen ---

        internal static List<Zeile> Lies(int projektId, int komponentenId, int kategorieId)
        {
            // Bestandssignatur: ALLE Positionen der Komponente (Rechen-/Smokewege).
            return Lies(projektId, komponentenId, kategorieId, -1);
        }

        /// <summary>Ä20: Positionen EINER Anlage (<paramref name="idAnlage"/> &gt; 0),
        /// der „ohne Anlagenzuordnung“-Pflege (0: NULL oder verwaiste Verweise)
        /// oder aller Anlagen (-1). Auf einer Datenbank ohne die Spalte fällt der
        /// Filter weg (Bestandsverhalten der Vorsorge).</summary>
        internal static List<Zeile> Lies(int projektId, int komponentenId, int kategorieId,
                                         int idAnlage)
        {
            var liste = new List<Zeile>();
            Dictionary<int, KostenPositionCtrl.Zusatz> zusaetze =
                KostenPositionCtrl.LiesZusatz(projektId, kategorieId);

            bool spalteDa = false;
            try { spalteDa = KostenPositionCtrl.StelleSpaltenSicher(); } catch { }

            string filter = "";
            var parameter = new List<DbParam>
            {
                new DbParam("@p", projektId),
                new DbParam("@k", komponentenId),
                new DbParam("@g", kategorieId)
            };
            if (spalteDa && idAnlage > 0)
            {
                filter = "AND w.ID_Anlage = ? ";
                parameter.Add(new DbParam("@a", idAnlage));
            }
            else if (spalteDa && idAnlage == 0)
            {
                // NULL oder Verweis auf eine geloeschte Anlage — beides ist die
                // „ohne Anlagenzuordnung“-Pflege der Oberflaeche. Die Projekt-Id
                // steht als LITERAL in der Unterabfrage: ACE bindet positionale
                // Parameter bei Unterabfragen in falscher Reihenfolge, die Abfrage
                // traefe still 0 Zeilen (Ä21-Befund, dieselbe Falle wie beim
                // UPDATE mit Unterabfrage).
                filter = "AND (w.ID_Anlage IS NULL OR w.ID_Anlage NOT IN " +
                         "(SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = " +
                         projektId + ")) ";
            }

            DataTable dt = DataRepository.GetDataTable(
                "SELECT w.ID, w.StammID, w.EingegebenerWert, w.Nutzungsdauer, " +
                "w.BestCase, w.WorstCase, w.BestCase_Nutzungsdauer, w.WorstCase_Nutzungsdauer, " +
                "k.Bezeichnung " +
                "FROM Tab_ProjektWerte AS w INNER JOIN Tab_Kostenfaktor AS k " +
                "ON w.StammID = k.StammID " +
                "WHERE w.ProjektID = ? AND w.KomponentenID = ? AND w.KategorieID = ? " +
                filter +
                "ORDER BY w.ID",
                parameter.ToArray());
            if (dt == null) return liste;

            // ANWENDERBEFUND W5‑B‑7 (08.09.2026) — DER EINE RECHENWEG.
            // Bis hierher nahm der Dialog je Zeile nur die GESPEICHERTE Menge. Für
            // „% der Investition" wird in Kategorie 1 nie eine gespeichert
            // (WirtschaftlichkeitCtrl.MengeAusweisen schließt die Kombination
            // ausdrücklich aus) — BetriebskostenCtrl.Betrag fiel deshalb auf den
            // eingegebenen Wert 0 zurück, und die Prozentzeilen standen auf 0, während
            // die Kapitalwertrechnung längst 6.961,80 € rechnete. Jetzt fragt der Dialog
            // DIESELBE Kaskade (Kategorie 1) bzw. dieselbe Nachweisliste (Kategorie 2)
            // wie der Rechenkern. Ist dort nichts zu holen (Datenbank ohne Schritt 19),
            // bleibt der Bestandsweg darunter stehen.
            Dictionary<int, InvestKaskade.Zeile> kaskade = null;
            Dictionary<int, KostenPositionNachweis> betrieb = null;
            try
            {
                if (kategorieId == DbWerte.KOSTEN_KATEGORIE_INVESTITION)
                    kaskade = InvestKaskade.NachId(projektId, WirtschaftlichkeitSzenario.ERWARTET);
                else if (kategorieId == DbWerte.KOSTEN_KATEGORIE_BETRIEB)
                    betrieb = WirtschaftlichkeitCtrl.BetriebNachId(
                        projektId, WirtschaftlichkeitSzenario.ERWARTET);
            }
            catch { }

            foreach (DataRow r in dt.Rows)
            {
                var z = new Zeile();
                z.ProjektId = projektId;
                z.KategorieId = kategorieId;
                z.Raster.Id = Convert.ToInt32(r["ID"]);
                z.Raster.StammId = r["StammID"] == DBNull.Value
                    ? (int?)null : Convert.ToInt32(r["StammID"]);
                z.Raster.Bezeichnung = Convert.ToString(r["Bezeichnung"]);
                z.Eingegeben = W(r, "EingegebenerWert") ?? 0;
                z.Raster.Nutzungsdauer = W(r, "Nutzungsdauer");
                z.Best = W(r, "BestCase") ?? 0;
                z.Worst = W(r, "WorstCase") ?? 0;
                z.BestNutzung = W(r, "BestCase_Nutzungsdauer") ?? 0;
                z.WorstNutzung = W(r, "WorstCase_Nutzungsdauer") ?? 0;

                KostenPositionCtrl.Zusatz zu;
                if (!zusaetze.TryGetValue(z.Raster.Id, out zu)) zu = new KostenPositionCtrl.Zusatz();
                z.Raster.Kostenart = zu.Kostenart ?? "";
                z.Raster.Bemessung = string.IsNullOrEmpty(zu.Bemessung)
                    ? DbWerte.BEMESSUNG_BETRAG : zu.Bemessung;
                z.Raster.IstErloes = zu.IstErloes;
                z.Menge = zu.Menge;
                z.StartJahr = zu.StartJahr;

                // Feldsemantik der Materialisierung (KD3): absolut → der Satz IST
                // der eingegebene Wert; satzbasiert → Satz = Einheitpreis.
                BemessungKatalog.Info info = BemessungKatalog.Finde(z.Raster.Bemessung);
                bool absolut = info == null || info.Absolut;
                z.Raster.Satz = absolut ? (double?)z.Eingegeben : zu.Einheitpreis;

                // Anzeigebetrag aus dem EINEN Rechenweg (auch für Kategorie 1 —
                // die Bemessungsarten sind dieselben 15 des Katalogs).
                double betrag;
                try { betrag = BetriebskostenCtrl.Betrag(z.Eingegeben, zu); }
                catch { betrag = z.Eingegeben; }
                z.Raster.BetragNetto = betrag;

                // W5‑B‑7: Wo der Rechenkern die Zeile kennt, gilt SEINE Zahl — samt
                // der Bezugsgröße, mit der sie entstanden ist.
                InvestKaskade.Zeile k;
                KostenPositionNachweis n;
                int anlageDerZeile = idAnlage > 0 ? idAnlage : 0;
                if (kaskade != null && kaskade.TryGetValue(z.Raster.Id, out k))
                {
                    z.Raster.BetragNetto = k.Betrag;
                    z.Basis = k.Basis;
                    anlageDerZeile = k.Anlage;
                    // U28: Runde und Herkunft schreibt die Kaskade beim Ableiten mit —
                    // der Dialog nennt sie unter dem Betrag, statt sie nachzubilden.
                    z.Runde = k.Runde;
                    z.BasisHerkunft = k.Herkunft ?? "";
                }
                else if (betrieb != null && betrieb.TryGetValue(z.Raster.Id, out n))
                {
                    z.Raster.BetragNetto = n.BetragJahr;
                    z.Basis = n.Menge;
                    anlageDerZeile = n.Anlage;
                    // U28: Die Betriebsseite hat keine Kaskade (Runde bleibt 0). Ihre
                    // Bezugsgröße ist die Investitionssumme (H4a), eine BAUGRÖSSE der
                    // Anlage oder eine Menge des Simulationslaufs.
                    //
                    // U33 (18.09.2026): Die dritte Quelle war bis hierher nicht benannt —
                    // jede nicht-prozentuale Betriebszeile galt als „Lauf". Mit „je kWp
                    // Leistung" im Betriebsraster stünde an einer Wartungszeile „· Lauf",
                    // obwohl die kWp aus der Gerätewelt kommen und kein Lauf sie liefert.
                    // Gefragt wird dieselbe Landkarte, aus der die Menge selbst kommt
                    // (WirtschaftlichkeitCtrl.RueckfallMenge → TechnikPlanwertCtrl):
                    // Kennt das Gewerk zu dieser Art eine Baugröße, ist die Herkunft die
                    // ANLAGE, sonst der Lauf.
                    z.BasisHerkunft = !z.Basis.HasValue ? ""
                        : WirtschaftlichkeitCtrl.IstProzentInvest(z.Raster.Bemessung)
                            ? KostenHerleitung.HERKUNFT_INVEST
                            : TechnikPlanwertCtrl.KenntBaugroesse(komponentenId, z.Raster.Bemessung)
                                ? KostenHerleitung.HERKUNFT_ANLAGE
                                : KostenHerleitung.HERKUNFT_LAUF;
                }

                // ANWENDERBEFUND 14.09.2026: Eine GERECHNETE Bezugsgröße nennt ihre
                // Formel — sonst stünden im Raster 17,50 kW, die in keiner
                // Gerätemaske zu finden sind.
                z.BasisHerleitung = z.Basis.HasValue
                    ? TechnikPlanwertCtrl.BaugroesseHerleitung(
                          projektId, komponentenId, z.Raster.Bemessung, anlageDerZeile)
                    : "";

                // ANWENDERBEFUND 10.09.2026 (H4c): Steht keine Bezugsgröße, wird der
                // GRUND mitgegeben. Ohne ihn zeigt das Raster nur die 0 des
                // Anwenderentscheids I-2 — und der Anwender sucht den Fehler bei sich.
                z.BasisGrund = z.Basis.HasValue
                    ? "" : WirtschaftlichkeitCtrl.BasisGrund(z.Raster.Bemessung, komponentenId);

                liste.Add(z);
            }
            return liste;
        }

        // ---------------------------------------------------------- Schreiben ---

        /// <summary>Felder einer bestehenden Zeile sichern (Bezeichnung über das
        /// Positionslexikon, Bemessung/Satz über den Zusatz-Schreibweg, ND direkt).</summary>
        internal static bool Speichern(Zeile z)
        {
            if (z == null || z.Raster.Id <= 0) return false;

            // Bezeichnung = Lexikonverweis: Umbenennen heißt StammID wechseln.
            int stammId = KostenVorlagenUebernahmeCtrl.StammIdSicher(z.Raster.Bezeichnung);
            if (stammId > 0 && (!z.Raster.StammId.HasValue || z.Raster.StammId.Value != stammId))
            {
                DataRepository.ExecuteSQL(
                    "UPDATE Tab_ProjektWerte SET StammID = ? WHERE ID = ?",
                    new DbParam("@s", stammId),
                    new DbParam("@id", z.Raster.Id));
                z.Raster.StammId = stammId;
            }

            BemessungKatalog.Info info = BemessungKatalog.Finde(z.Raster.Bemessung);
            bool absolut = info == null || info.Absolut;
            double eingegeben = absolut ? (z.Raster.Satz ?? 0) : 0.0;

            var zusatz = new KostenPositionCtrl.Zusatz
            {
                Kostenart = z.Raster.Kostenart ?? "",
                Bemessung = z.Raster.Bemessung,
                IstErloes = z.Raster.IstErloes,
                Menge = z.Menge,
                Einheitpreis = absolut ? (double?)null : z.Raster.Satz,
                StartJahr = z.StartJahr
            };
            if (!KostenPositionCtrl.SetzeBetragMitZusatz(z.Raster.Id, eingegeben, zusatz))
                return false;
            z.Eingegeben = eingegeben;

            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET Nutzungsdauer = ? WHERE ID = ?",
                Zahl("@n", z.Raster.Nutzungsdauer),
                new DbParam("@id", z.Raster.Id));

            // ETAPPE H2-1 (Konzept BHKW-Wirtschaftlichkeit § 4.5): für ermittelbare
            // Bemessungsarten weist das Speichern den FRISCHEN Stand der Bezugsgröße
            // in Tab_ProjektWerte.Menge aus („Stand des Laufs"). Die Rechenwege lesen
            // ohnehin frisch — der Ausweis versorgt Dialoganzeige und Fremdleser.
            double? ausweis;
            if (WirtschaftlichkeitCtrl.MengeAusweisen(z.Raster.Id, out ausweis))
            {
                z.Menge = ausweis;
                zusatz.Menge = ausweis;
            }

            KostenPositionCtrl.Zusatz zu2 = zusatz;
            double betrag;
            try { betrag = BetriebskostenCtrl.Betrag(eingegeben, zu2); }
            catch { betrag = eingegeben; }
            z.Raster.BetragNetto = betrag;

            // W5‑B‑7: Nach dem Schreiben gilt wieder der Rechenweg des Kerns — sonst
            // stünde in der Zeile bis zum nächsten Laden der Wert, den der Dialog selbst
            // ausgerechnet hat (bei „% der Investition" die 0). Der Dialog lädt nach
            // „Speichern" ohnehin neu; diese Zeile macht das Objekt schon vorher ehrlich.
            NachziehenAusRechenweg(z);
            return true;
        }

        /// <summary>
        /// W5‑B‑7: Zieht Betrag und Bezugsgröße EINER Zeile aus dem Rechenweg des
        /// Kerns nach (Kaskade bzw. Nachweisliste). Still — findet er die Zeile nicht
        /// (Datenbank ohne Schritt 19, fremde Kategorie), bleibt der zuvor errechnete
        /// Wert stehen.
        /// </summary>
        private static void NachziehenAusRechenweg(Zeile z)
        {
            if (z == null || z.ProjektId <= 0 || z.Raster.Id <= 0) return;
            try
            {
                if (z.KategorieId == DbWerte.KOSTEN_KATEGORIE_INVESTITION)
                {
                    InvestKaskade.Zeile k;
                    if (InvestKaskade.NachId(z.ProjektId, WirtschaftlichkeitSzenario.ERWARTET)
                            .TryGetValue(z.Raster.Id, out k))
                    { z.Raster.BetragNetto = k.Betrag; z.Basis = k.Basis; }
                }
                else if (z.KategorieId == DbWerte.KOSTEN_KATEGORIE_BETRIEB)
                {
                    KostenPositionNachweis n;
                    if (WirtschaftlichkeitCtrl.BetriebNachId(z.ProjektId, WirtschaftlichkeitSzenario.ERWARTET)
                            .TryGetValue(z.Raster.Id, out n))
                    { z.Raster.BetragNetto = n.BetragJahr; z.Basis = n.Menge; }
                }
            }
            catch { }

            int komponente, anlage;
            Zeilenbezug(z, out komponente, out anlage);

            // H4c: derselbe Grundausweis wie beim Laden.
            z.BasisGrund = z.Basis.HasValue
                ? "" : WirtschaftlichkeitCtrl.BasisGrund(z.Raster.Bemessung, komponente);

            // 14.09.2026: und dieselbe Herleitung.
            z.BasisHerleitung = z.Basis.HasValue
                ? TechnikPlanwertCtrl.BaugroesseHerleitung(
                      z.ProjektId, komponente, z.Raster.Bemessung, anlage)
                : "";
        }

        /// <summary>H4c: Gewerk und Anlage der Zeile — <see cref="Speichern"/> kennt
        /// beides nicht als Parameter, Grundausweis und Herleitung brauchen es.
        /// 0 = unbekannt (dann nennt der Grund die Art, nicht das Gewerk, und die
        /// Herleitung bemisst sich am ganzen Projekt).</summary>
        private static void Zeilenbezug(Zeile z, out int komponente, out int anlage)
        {
            komponente = 0; anlage = 0;
            if (z == null || z.Raster.Id <= 0) return;
            try
            {
                bool mitAnlage = WirtschaftlichkeitCtrl.AnlagenSpalteVorhanden();
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT KomponentenID" +
                    (mitAnlage ? ", [" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "] " : " ") +
                    "FROM Tab_ProjektWerte WHERE ID = ?",
                    new DbParam("@id", z.Raster.Id));
                if (dt == null || dt.Rows.Count == 0) return;
                DataRow r = dt.Rows[0];
                if (r["KomponentenID"] != DBNull.Value)
                    komponente = Convert.ToInt32(r["KomponentenID"]);
                if (mitAnlage && r[SchemaKatalog.SPALTE_PW_ID_ANLAGE] != DBNull.Value)
                    anlage = Convert.ToInt32(r[SchemaKatalog.SPALTE_PW_ID_ANLAGE]);
            }
            catch { }
        }

        /// <summary>
        /// ANWENDERBEFUND 10.09.2026 (H4c): die frische Bezugsgröße einer Zeile zu einer
        /// NOCH NICHT gespeicherten Bemessungsart — für die Sofortrechnung des Dialogs
        /// beim Wechsel der Bemessung. Gerechnet wird im Kern
        /// (<see cref="WirtschaftlichkeitCtrl.FrischeBasis"/>), geschrieben nichts.
        /// </summary>
        internal static void BasisNachziehen(Zeile z, string bemessung)
        {
            if (z == null || z.Raster.Id <= 0) return;
            string grund;
            z.Basis = WirtschaftlichkeitCtrl.FrischeBasis(z.Raster.Id, bemessung, out grund);
            z.BasisGrund = grund;

            // 14.09.2026: Die Herleitung folgt der NEUEN Art — wer von „je m²" auf
            // „je kW Leistung" wechselt, sieht sofort, woher die kW kommen.
            int komponente, anlage;
            Zeilenbezug(z, out komponente, out anlage);
            z.BasisHerleitung = z.Basis.HasValue
                ? TechnikPlanwertCtrl.BaugroesseHerleitung(
                      z.ProjektId > 0 ? z.ProjektId : ProjektDerZeile(z), komponente,
                      bemessung, anlage)
                : "";
        }

        /// <summary>Das Projekt der Zeile, wenn der Aufrufer es nicht mitgeführt hat
        /// (Bestandsweg über <c>Tab_ProjektWerte</c>). 0 = unauffindbar.</summary>
        private static int ProjektDerZeile(Zeile z)
        {
            if (z == null || z.Raster.Id <= 0) return 0;
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT ProjektID FROM Tab_ProjektWerte WHERE ID = ?",
                    new DbParam("@id", z.Raster.Id));
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
            }
            catch { return 0; }
        }

        /// <summary>Bestandssignatur — Position ohne Anlagenbezug.</summary>
        internal static int Neu(int projektId, int komponentenId, int kategorieId,
                                string name, string kostenart, string bemessung)
        {
            return Neu(projektId, komponentenId, kategorieId, name, kostenart, bemessung, 0);
        }

        /// <summary>Ä20: die Anlagenzeile, zu der eine Position gehört, nachtragen.
        /// Still — die Vorsorge legt die Spalte an; scheitert sie, bleibt die
        /// Position komponentenweit (Ausweis „ohne Anlagenzuordnung“).</summary>
        internal static void AnlageZuordnen(int idPosition, int idAnlage)
        {
            if (idPosition <= 0 || idAnlage <= 0) return;
            bool spalteDa = false;
            try { spalteDa = KostenPositionCtrl.StelleSpaltenSicher(); } catch { }
            if (!spalteDa) return;
            DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET ID_Anlage = ?, ID_AnlageGeraet = ? WHERE ID = ?",
                new DbParam("@a", idAnlage),
                Zahl("@g", GeraetDerAnlage(idAnlage)),
                new DbParam("@id", idPosition));
        }

        /// <summary>Ä21: der Gerätewert der Anlagenzeile (erste gesetzte
        /// Verweisspalte) — der wizardfeste Anker; null = keiner.</summary>
        internal static double? GeraetDerAnlage(int idAnlage)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID_WP, ID_Kessel, ID_BHKW, ID_PV, ID_Solar, ID_SP, ID_PUFFER " +
                    "FROM Tab_Energieanlagen WHERE ID = ?",
                    new DbParam("@id", idAnlage));
                if (dt == null || dt.Rows.Count == 0) return null;
                foreach (string s in new[] { "ID_WP", "ID_Kessel", "ID_BHKW", "ID_PV",
                                             "ID_Solar", "ID_SP", "ID_PUFFER" })
                {
                    object o = dt.Rows[0][s];
                    if (o != DBNull.Value && Convert.ToInt32(o) > 0) return Convert.ToInt32(o);
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Ä24: Leitet den GERÄTEANKER aller GÜLTIG zugeordneten Positionen des
        /// Projekts aus ihrer Anlagenzeile neu ab (Überschreiben mit der Wahrheit;
        /// idempotent). Anlass: Der Projektduplizierer versetzt <c>ID_Anlage</c>
        /// (FK_MAP), den komponentenabhängigen Anker kann er nicht kennen —
        /// Kopien ankerten an den Geräten des QUELLprojekts und verloren die
        /// Zuordnung beim ersten Anlagen-Wizard-Lauf der Variante.
        /// Migrationsschritt 47 macht dasselbe einmalig für den Bestand.
        ///
        /// <para>Je Komponente laufen ZWEI Anweisungen: Die erste leitet den Anker der
        /// gültig zugeordneten Zeilen aus ihrer Anlagenzeile ab, die zweite setzt ihn bei
        /// den Zeilen OHNE gültige Anlage auf <c>NULL</c>. Ohne die zweite überlebte der
        /// Anker des Quellprojekts jede Kopie — ein toter Verweis auf ein Gerät, das es im
        /// Zielprojekt nicht gibt.</para>
        /// </summary>
        internal static void AnkerNachziehen(int projektId)
        {
            if (projektId <= 0) return;
            bool spalteDa = false;
            try { spalteDa = KostenPositionCtrl.StelleSpaltenSicher(); } catch { }
            if (!spalteDa) return;

            var namen = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { DbWerte.ERZEUGER_WAERMEPUMPE,             "ID_WP" },
                { DbWerte.ERZEUGER_HEIZKESSEL,              "ID_Kessel" },
                { DbWerte.ERZEUGER_BHKW,                    "ID_BHKW" },
                { DbWerte.ERZEUGER_PHOTOVOLTAIK,            "ID_PV" },
                { DbWerte.ERZEUGER_SOLARTHERMIE,            "ID_Solar" },
                { DbWerte.ERZEUGER_STROMSPEICHER,           "ID_SP" },
                { DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER, "ID_PUFFER" }
            };
            try
            {
                DataTable komp = DataRepository.GetDataTable(
                    "SELECT ID, Komponente FROM Tab_KostenKomponente");
                if (komp == null) return;
                foreach (DataRow r in komp.Rows)
                {
                    string sp;
                    if (!namen.TryGetValue(Convert.ToString(r["Komponente"]), out sp)) continue;
                    // Ids als LITERALE (ACE-Bindungsfalle, Ä21-Befund); der
                    // ID_Projekt-Vergleich schützt vor Fremdzuordnungen.
                    // SQLite (Cutover 02.09.2026, Befund FS1 N-1): den Access-Verbund
                    // "UPDATE ... INNER JOIN ... SET" kennt SQLite nicht (near "INNER":
                    // syntax error - je Komponente ein Fehlerdialog bei jedem Speichern
                    // der Erzeugerliste). Ersatz mit demselben Ergebnis: korreliertes
                    // UPDATE - der Wert kommt per Unterabfrage aus der Anlagenzeile
                    // (a.ID ist Schluessel, hoechstens eine Zeile), die Treffermenge
                    // schraenkt EXISTS auf dieselbe Verbundbedingung ein. Positionen
                    // ohne (gueltige) Anlage dieses Projekts bleiben unberuehrt.
                    // Schritt 47 im eingefrorenen Access-Zweig behaelt seine JOIN-Form.
                    // Zweiter Fundort desselben Fehlers (remote dd4113f, SQL-Dialekt-
                    // Audit): schon das ANLEGEN eines Heizkessels loeste ihn aus.
                    int komponentenId = Convert.ToInt32(r["ID"]);
                    string anlageDesProjekts =
                        "FROM Tab_Energieanlagen AS a " +
                        "WHERE a.ID = Tab_ProjektWerte.ID_Anlage AND a.ID_Projekt = " + projektId;
                    DataRepository.ExecuteSQL(
                        "UPDATE Tab_ProjektWerte SET ID_AnlageGeraet = " +
                        "(SELECT a.[" + sp + "] " + anlageDesProjekts + ") " +
                        "WHERE ProjektID = " + projektId +
                        " AND KomponentenID = " + komponentenId +
                        " AND EXISTS (SELECT 1 " + anlageDesProjekts + ")");

                    // Die GEGENPROBE zum Satz darüber (Auftrag Kostenbereich,
                    // Nebenbefund 2): Die EXISTS-Klausel zieht den Anker nur für
                    // Zeilen mit gültiger Anlage nach — eine Position, die schon in
                    // der QUELLE lose war (ID_Anlage NULL oder verwaister Verweis),
                    // behielt den Geräteanker des Quellprojekts für immer. In der
                    // Kopie zeigt er auf ein Gerät, das es dort nicht gibt: ein
                    // toter Verweis, den kein Leseweg mehr auflösen kann. Eine
                    // Position ohne gültige Anlage hat keinen Anker — sie bekommt
                    // NULL, wie sie ihn auch in ZuordnungReparieren bekäme, wenn ihr
                    // Gerät verschwindet ("Zuordnung EHRLICH lösen").
                    DataRepository.ExecuteSQL(
                        "UPDATE Tab_ProjektWerte SET ID_AnlageGeraet = NULL " +
                        "WHERE ProjektID = " + projektId +
                        " AND KomponentenID = " + komponentenId +
                        " AND ID_AnlageGeraet IS NOT NULL" +
                        " AND NOT EXISTS (SELECT 1 " + anlageDesProjekts + ")");
                }
            }
            catch { }
        }

        /// <summary>
        /// Ä21: SELBSTHEILUNG der Anlagenzuordnung. Der Anlagen-Wizard löscht
        /// Anlagenzeilen und legt sie mit NEUEN IDs an (dokumentiert in
        /// <c>AnlagenEindeutigkeit</c>) — Positionen zeigten danach ins Leere.
        /// Verwaiste Zuordnungen werden hier über Komponente + Geräteanker auf die
        /// neue Anlagenzeile umgeschlüsselt; ohne Treffer (Gerät entfernt) bleibt
        /// die Position „ohne Anlagenzuordnung“ sichtbar. Läuft vor jedem
        /// UI-Aufbau (Kosten-Seite, Kostenverwaltung) — ein COUNT bei gesundem
        /// Bestand.
        /// </summary>
        internal static void ZuordnungReparieren(int projektId)
        {
            if (projektId <= 0) return;
            bool spalteDa = false;
            try { spalteDa = KostenPositionCtrl.StelleSpaltenSicher(); } catch { }
            if (!spalteDa) return;

            // Projekt-Id als LITERAL: ACE bindet Parameter bei Unterabfragen in
            // falscher Reihenfolge (Ä21-Befund).
            object n = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = " + projektId +
                " AND ID_Anlage IS NOT NULL AND ID_Anlage NOT IN " +
                "(SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = " + projektId + ")");
            if (n == null || n == DBNull.Value || Convert.ToInt32(n) == 0) return;

            // Landkarte Komponente -> Verweisspalte (dieselbe wie Migration 45/46).
            var verweise = new Dictionary<int, string>();
            try
            {
                var namen = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { DbWerte.ERZEUGER_WAERMEPUMPE,             "ID_WP" },
                    { DbWerte.ERZEUGER_HEIZKESSEL,              "ID_Kessel" },
                    { DbWerte.ERZEUGER_BHKW,                    "ID_BHKW" },
                    { DbWerte.ERZEUGER_PHOTOVOLTAIK,            "ID_PV" },
                    { DbWerte.ERZEUGER_SOLARTHERMIE,            "ID_Solar" },
                    { DbWerte.ERZEUGER_STROMSPEICHER,           "ID_SP" },
                    { DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER, "ID_PUFFER" }
                };
                DataTable komp = DataRepository.GetDataTable(
                    "SELECT ID, Komponente FROM Tab_KostenKomponente");
                if (komp != null)
                    foreach (DataRow r in komp.Rows)
                    {
                        string sp;
                        if (namen.TryGetValue(Convert.ToString(r["Komponente"]), out sp))
                            verweise[Convert.ToInt32(r["ID"])] = sp;
                    }
            }
            catch { return; }

            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, KomponentenID, ID_AnlageGeraet FROM Tab_ProjektWerte " +
                "WHERE ProjektID = " + projektId + " AND ID_Anlage IS NOT NULL AND " +
                "ID_Anlage NOT IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = " +
                projektId + ")");
            if (dt == null) return;

            var neueAnlage = new Dictionary<string, object>();   // "kid|geraet" -> ID oder DBNull
            foreach (DataRow r in dt.Rows)
            {
                int kid = r["KomponentenID"] == DBNull.Value ? 0 : Convert.ToInt32(r["KomponentenID"]);
                string sp2;
                if (!verweise.TryGetValue(kid, out sp2)) continue;

                object geraet = r["ID_AnlageGeraet"];
                string schluessel = kid + "|" + (geraet == DBNull.Value ? "-" : geraet.ToString());
                object ziel;
                if (!neueAnlage.TryGetValue(schluessel, out ziel))
                {
                    ziel = DBNull.Value;
                    if (geraet != DBNull.Value)
                        ziel = DataRepository.ExecuteScalar(
                            "SELECT MIN(ID) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND [" +
                            sp2 + "] = ?",
                            new DbParam("@p", projektId),
                            new DbParam("@g", Convert.ToInt32(geraet)));
                    neueAnlage[schluessel] = ziel ?? DBNull.Value;
                }

                if (ziel == null || ziel == DBNull.Value)
                {
                    // Gerät existiert nicht mehr im Projekt: Zuordnung EHRLICH lösen —
                    // die Position erscheint als „ohne Anlagenzuordnung“.
                    DataRepository.ExecuteSQL(
                        "UPDATE Tab_ProjektWerte SET ID_Anlage = NULL WHERE ID = ?",
                        new DbParam("@id", Convert.ToInt32(r["ID"])));
                }
                else
                    DataRepository.ExecuteSQL(
                        "UPDATE Tab_ProjektWerte SET ID_Anlage = ? WHERE ID = ?",
                        new DbParam("@a", Convert.ToInt32(ziel)),
                        new DbParam("@id", Convert.ToInt32(r["ID"])));
            }
        }

        /// <summary>
        /// Was der Papierkorb der gelben Zeile wegnehmen WÜRDE: Anzahl und Summe der
        /// Positionen ohne (gültige) Anlagenzuordnung.
        /// </summary>
        internal struct LoseBefund
        {
            /// <summary>Zahl der Positionen, die <see cref="LoseLoeschen"/> träfe.</summary>
            public int Anzahl;

            /// <summary>Ihre Summe [€] — der BERECHNETE Betrag, nicht der eingegebene.</summary>
            public double Summe;
        }

        /// <summary>
        /// Zählt VOR dem Löschen, was der Papierkorb der gelben Zeile träfe (Anzahl und
        /// Summe), damit die Rückfrage beides nennen kann.
        ///
        /// <para><b>Dieselbe Menge wie <see cref="LoseLoeschen"/>:</b> gezählt wird über
        /// <see cref="Lies(int,int,int,int)"/> mit <c>idAnlage = 0</c> — der einen Stelle,
        /// die „ohne Anlagenzuordnung" definiert (<c>ID_Anlage IS NULL</c> oder Verweis auf
        /// eine Anlage, die es im Projekt nicht mehr gibt). Eine zweite Abfrage mit
        /// eigenem Filter ginge irgendwann auseinander.</para>
        ///
        /// <para><b>Die Summe ist der ANGEZEIGTE Betrag</b> (<c>Raster.BetragNetto</c>) —
        /// derselbe, der in der gelben Zeile steht und den die Wirtschaftlichkeit rechnet,
        /// nicht der rohe <c>EingegebenerWert</c>. Für eine satzbasierte Position sind das
        /// zwei verschiedene Zahlen.</para>
        /// </summary>
        /// <param name="kategorieId">
        /// <c>DbWerte.KOSTEN_KATEGORIE_INVESTITION</c> oder <c>…_BETRIEB</c>; ein Wert
        /// ≤ 0 zählt BEIDE Kategorien — also genau das, was <see cref="LoseLoeschen"/>
        /// löscht.
        /// </param>
        internal static LoseBefund LoseZaehlen(int projektId, int komponentenId, int kategorieId)
        {
            var befund = new LoseBefund();
            if (projektId <= 0 || komponentenId <= 0) return befund;

            int[] kategorien = kategorieId > 0
                ? new[] { kategorieId }
                : new[] { DbWerte.KOSTEN_KATEGORIE_INVESTITION, DbWerte.KOSTEN_KATEGORIE_BETRIEB };

            foreach (int kategorie in kategorien)
                foreach (Zeile z in Lies(projektId, komponentenId, kategorie, 0))
                {
                    befund.Anzahl++;
                    // BetragNetto ist nullbar: eine Position ohne gepflegten Wert zählt
                    // mit, trägt aber 0 zur Summe bei — gezählt wird, was gelöscht wird.
                    befund.Summe += z.Raster.BetragNetto ?? 0.0;
                }
            return befund;
        }

        /// <summary>
        /// <c>Tab_KostenKomponente.ID</c> zum sprachneutralen Gewerkenamen; 0, wenn es ihn
        /// nicht gibt. Die Oberfläche kennt die Komponente als NAMEN — die Id dazu holt der
        /// Kern, nicht die Hülle.
        /// </summary>
        internal static int KomponentenId(string komponente)
        {
            if (string.IsNullOrEmpty(komponente)) return 0;
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT ID FROM Tab_KostenKomponente WHERE Komponente = ?",
                    new DbParam("@k", komponente));
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
            catch { return 0; }
        }

        /// <summary>Ä21: alle Positionen einer Komponente OHNE (gültige)
        /// Anlagenzuordnung löschen — der Aufräumweg der gelben Zeilen der
        /// Kosten-Seite (z. B. Variantenreste eines nicht übernommenen Gewerks).
        /// Läuft über die Einzellöschung (keine Unterabfragen im DELETE —
        /// ACE-Falle).</summary>
        internal static int LoseLoeschen(int projektId, int komponentenId)
        {
            int geloescht = 0;
            foreach (int kategorie in new[] { DbWerte.KOSTEN_KATEGORIE_INVESTITION,
                                              DbWerte.KOSTEN_KATEGORIE_BETRIEB })
                foreach (Zeile z in Lies(projektId, komponentenId, kategorie, 0))
                    if (VerwaisteLoeschen(z.Raster.Id)) geloescht++;
            return geloescht;
        }

        /// <summary>
        /// Windows-Abnahme 08.09.2026 (W5‑B‑6, „das Löschen der gelb hinterlegten Anlage ohne
        /// Zuordnung funktioniert nicht"): Die losen Positionen sind zum größten Teil
        /// PFLICHTPOSITIONEN (H3) einer Anlagenzeile, die es nicht mehr gibt — der Anwender hat
        /// die Wärmepumpe getauscht, der Del+Add-Speicherweg legte die Zeile neu an, die Heilung
        /// über den Geräteanker fand kein Ziel (anderes Gerät), und
        /// <c>PflichtpositionenSicherstellen</c> versorgte die neue Anlage mit eigenen
        /// Pflichtzeilen. <see cref="Loeschen"/> schützt Pflichtzeilen — zu Recht bei einer
        /// lebenden Anlage, hier aber der Grund, warum die gelbe Zeile blieb: 0 von 3 gelöscht,
        /// und die Fußzeile sagte es nur leise. Eine Position OHNE Anlage hat keinen
        /// Pflichtgrund mehr; die ausdrückliche Pflege „Positionen ohne Anlagenzuordnung
        /// löschen" darf sie wegnehmen. <see cref="Lies"/> mit 0 liefert nur solche Zeilen
        /// (NULL oder verwaister Verweis) — der Schutz der lebenden Anlagen bleibt.
        /// </summary>
        private static bool VerwaisteLoeschen(int id)
        {
            return id > 0 && DataRepository.ExecuteSQL(
                "DELETE FROM Tab_ProjektWerte WHERE ID = ?",
                new DbParam("@id", id));
        }

        /// <summary>Neue Projektposition (Muster der Übernahme-Mechanik, § 8);
        /// Ä20: mit Anlagenbezug (<paramref name="idAnlage"/> 0 = ohne).</summary>
        internal static int Neu(int projektId, int komponentenId, int kategorieId,
                                string name, string kostenart, string bemessung, int idAnlage)
        {
            int stammId = KostenVorlagenUebernahmeCtrl.StammIdSicher(name);
            if (stammId <= 0) return 0;

            string gruppe = kategorieId == DbWerte.KOSTEN_KATEGORIE_BETRIEB
                ? DbWerte.KOSTEN_GRUPPE_BETRIEB_VDI
                : DbWerte.KOSTEN_GRUPPE_ALLGEMEIN;
            // Ä25: MIT Anlagenbezug anlegen. Die anlagenblinde Bestandssignatur fand
            // bei mehreren Anlagen derselben Komponente (Regelfall Pufferspeicher)
            // die Position der ERSTEN Anlage, nullte ihren Betrag und hängte sie über
            // AnlageZuordnen an die neue — die Kosten der ersten Anlage waren weg.
            int id = KostenPositionCtrl.SetzeBetrag(projektId, kategorieId,
                komponentenId, stammId, 0.0, gruppe, true, idAnlage);
            if (id <= 0) return 0;

            KostenPositionCtrl.SetzeBetragMitZusatz(id, 0.0, new KostenPositionCtrl.Zusatz
            {
                Kostenart = kostenart ?? "",
                Bemessung = string.IsNullOrEmpty(bemessung) ? DbWerte.BEMESSUNG_BETRAG : bemessung,
                IstErloes = false,
                Menge = null,
                Einheitpreis = null
            });
            if (idAnlage > 0) AnlageZuordnen(id, idAnlage);

            // STUFE S1 (Konzept Nutzungsdauer/AfA 2.4.1): Eine neue INVESTITIONSposition
            // bekommt die Nutzungsdauer ihrer Technik - ohne Positionsart also die
            // Standardzeile. Sie entstuende sonst mit 0, und 0 rechnet im
            // KapitalwertRechner still "wie Betrachtungszeitraum". Vorbelegt wird nur
            // eine NEUE Zeile; kein gespeicherter Wert wird angefasst.
            if (kategorieId == DbWerte.KOSTEN_KATEGORIE_INVESTITION)
                NutzungsdauerVorbelegen(id, komponentenId, null);

            return id;
        }

        /// <summary>
        /// Schreibt die Nutzungsdauer-Vorgabe in eine FRISCHE Projektzeile — den Wert
        /// und beide Szenariospalten, wortgleich zur Vorlagenübernahme
        /// (<c>KostenVorlagenUebernahmeCtrl.HerkunftUndNutzungsdauer</c>, FK4/FK10).
        /// Gibt es keine Vorgabe, geschieht nichts: Es wird nichts erfunden.
        /// </summary>
        internal static void NutzungsdauerVorbelegen(int positionsId, int komponentenId,
                                                     int? nutzungsdauerId)
        {
            if (positionsId <= 0) return;

            NutzungsdauerVorgabe v = NutzungsdauerCtrl.Vorgabe(komponentenId, nutzungsdauerId);
            if (!v.Wert.HasValue) return;

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWerte SET Nutzungsdauer = ?, " +
                "BestCase_Nutzungsdauer = ?, WorstCase_Nutzungsdauer = ? WHERE ID = ?",
                new DbParam("@n1", v.Wert.Value),
                new DbParam("@n2", v.Wert.Value),
                new DbParam("@n3", v.Wert.Value),
                new DbParam("@id", positionsId));
        }

        /// <summary>
        /// Trägt die POSITIONSART einer Projektzeile nach (Stufe S1, Schritt 75) —
        /// <c>null</c> löscht sie. Still, wo es die Spalte noch nicht gibt: Die Zeile
        /// fällt dann auf den Technik-Standard zurück, und das ist derselbe Ausgang.
        /// </summary>
        internal static void NutzungsdauerArtZuordnen(int positionsId, int? nutzungsdauerId)
        {
            if (positionsId <= 0) return;
            if (!NutzungsdauerCtrl.VerweisSpalteVorhanden(SchemaKatalog.TAB_PROJEKTWERTE)) return;

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWerte SET [" + NutzungsdauerSchema.SPALTE_VERWEIS +
                "] = ? WHERE ID = ?",
                Ganzzahl("@art", nutzungsdauerId),
                new DbParam("@id", positionsId));
        }

        /// <summary>Nullbarer LONG-Parameter (Muster <c>KostenVorlagenCtrl.Ganz</c>).</summary>
        private static DbParam Ganzzahl(string name, int? wert)
        {
            var p = new DbParam(name, DbParamTyp.Integer);
            p.Wert = wert.HasValue ? (object)wert.Value : DBNull.Value;
            return p;
        }

        /// <summary>ETAPPE H3: Probe der Schritt-59-Spalte (Ergebnis je Prozess
        /// gemerkt, Muster <see cref="WirtschaftlichkeitCtrl.SpalteVorhanden"/>).</summary>
        private static bool? _pflichtSpalte;

        internal static bool PflichtSpalteVorhanden()
        {
            if (_pflichtSpalte.HasValue) return _pflichtSpalte.Value;
            _pflichtSpalte = WirtschaftlichkeitCtrl.SpalteVorhanden(
                SchemaKatalog.TAB_PROJEKTWERTE, SchemaKatalog.SPALTE_PW_IST_PFLICHT);
            return _pflichtSpalte.Value;
        }

        /// <summary>ETAPPE H3 (H1-2): true, wenn die Projektzeile eine
        /// Pflichtposition ist (Schritt 59). Fehlende Spalte oder Lesefehler
        /// bedeuten false — keine Sperre auf Verdacht.</summary>
        internal static bool IstPflicht(int id)
        {
            if (id <= 0 || !PflichtSpalteVorhanden()) return false;
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT [" + SchemaKatalog.SPALTE_PW_IST_PFLICHT +
                    "] FROM Tab_ProjektWerte WHERE ID = ?",
                    new DbParam("@id", id));
                return o != null && o != DBNull.Value && Convert.ToBoolean(o);
            }
            catch { return false; }
        }

        /// <summary>ETAPPE H3: Pflichtmerkmal einer Projektzeile setzen — die
        /// Übernahme reicht es aus der Vorlage durch (Lücke der H1-Saat:
        /// Schritt 59 markiert nur den Bestand, neue Übernahmen liefen sonst
        /// ohne Merkmal und die Löschsperre liefe ins Leere).</summary>
        internal static void PflichtSetzen(int id, bool pflicht)
        {
            if (id <= 0 || !PflichtSpalteVorhanden()) return;
            try
            {
                DataRepository.ExecuteSQL(
                    "UPDATE Tab_ProjektWerte SET [" + SchemaKatalog.SPALTE_PW_IST_PFLICHT +
                    "] = ? WHERE ID = ?",
                    new DbParam("@p", pflicht),
                    new DbParam("@id", id));
            }
            catch { }
        }

        /// <summary>Zeile löschen. ETAPPE H3 (H1-2): Pflichtpositionen sind
        /// gesperrt — die zweite Schicht neben der Dialogmeldung (dasselbe Doppel
        /// wie beim ReadOnly-Schutz der Kataloge); der Ausweg ist der Satz 0.</summary>
        internal static bool Loeschen(int id)
        {
            if (IstPflicht(id)) return false;
            return id > 0 && DataRepository.ExecuteSQL(
                "DELETE FROM Tab_ProjektWerte WHERE ID = ?",
                new DbParam("@id", id));
        }

        /// <summary>Worst/Best (Betrag + Nutzungsdauer) und Startjahr sichern
        /// (FK10; NULL bei Startjahr ≤ 1, dieselbe Regel wie der Kosteneditor).</summary>
        internal static bool CaseSichern(Zeile z)
        {
            if (z == null || z.Raster.Id <= 0) return false;
            bool ok = DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET BestCase = ?, WorstCase = ?, " +
                "BestCase_Nutzungsdauer = ?, WorstCase_Nutzungsdauer = ? WHERE ID = ?",
                new DbParam("@b", z.Best),
                new DbParam("@w", z.Worst),
                new DbParam("@bn", z.BestNutzung),
                new DbParam("@wn", z.WorstNutzung),
                new DbParam("@id", z.Raster.Id));
            if (!ok) return false;

            if (!KostenPositionCtrl.StelleSpaltenSicher()) return true;
            return DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWerte SET [" + SchemaKatalog.SPALTE_PW_STARTJAHR +
                "] = ? WHERE ID = ?",
                z.StartJahr > 1
                    ? new DbParam("@j", z.StartJahr)
                    : new DbParam("@j", DBNull.Value),
                new DbParam("@id", z.Raster.Id));
        }

        /// <summary>Kategoriesumme des Projekts über die BERECHNETEN Beträge —
        /// Erlöse negativ (L7), dieselbe Konvention wie der Summenfuß.</summary>
        internal static double Summe(List<Zeile> zeilen)
        {
            double netto = 0;
            if (zeilen != null)
                foreach (Zeile z in zeilen)
                    if (z.Raster.BetragNetto.HasValue)
                        netto += z.Raster.IstErloes
                            ? -z.Raster.BetragNetto.Value : z.Raster.BetragNetto.Value;
            return netto;
        }

        // -------------------------------------------------------------- Helfer ---

        private static double? W(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte]); } catch { return null; }
        }

        private static DbParam Zahl(string name, double? wert)
        {
            var p = new DbParam(name, DbParamTyp.Double);
            p.Wert = wert.HasValue ? (object)wert.Value : DBNull.Value;
            return p;
        }
    }
}
