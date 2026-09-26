using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WindowsFormsApplication1
{
    /// <summary>ETAPPE E8b (U43) — wie weit EPOS einen Punkt der Anhang-E-Checkliste liefert.</summary>
    public enum ChecklistenStand
    {
        /// <summary>EPOS liefert, was der Punkt verlangt.</summary>
        Erfuellt,

        /// <summary>EPOS liefert einen Teil; der Rest ist benannt.</summary>
        Teilweise,

        /// <summary>Es fehlt eine Angabe oder eine Rechnung.</summary>
        Offen
    }

    /// <summary>ETAPPE E8b (U43) — ein Punkt der Anhang-E-Checkliste.</summary>
    public sealed class ChecklistenPunkt
    {
        /// <summary>Nummer wie in der Norm („0.1", „2a", „11").</summary>
        public string Nummer = "";

        /// <summary>Die Gruppe (Gegenstand, A bis D) — bereits übersetzt.</summary>
        public string Gruppe = "";

        public string Thema = "";

        /// <summary>Was der Punkt verlangt — mit eigenen Worten, nicht der Normtext.</summary>
        public string Anforderung = "";

        /// <summary>Die Stelle im Bericht: Kapitel des Wortberichts und Blatt/Block der Mappe.</summary>
        public string Stelle = "";

        public ChecklistenStand Stand;

        /// <summary>Was EPOS zu dem Punkt liefert und was fehlt.</summary>
        public string StandText = "";

        /// <summary>Der Stand als Wort („erfüllt", „teilweise", „offen").</summary>
        public string StandWort
        {
            get
            {
                switch (Stand)
                {
                    case ChecklistenStand.Erfuellt: return MyResource.Resource.WIRT_AE_STAND_ERFUELLT;
                    case ChecklistenStand.Teilweise: return MyResource.Resource.WIRT_AE_STAND_TEILWEISE;
                    default: return MyResource.Resource.WIRT_AE_STAND_OFFEN;
                }
            }
        }

        /// <summary>Die Zelle „Stand in EPOS": Wort und Erläuterung („offen: nicht erfasst — …") —
        /// dieselbe Zeile im Wortbericht, in der Mappe und auf der Ergebnisseite.</summary>
        public string StandZeile { get { return StandWort + ": " + StandText; } }
    }

    /// <summary>
    /// ETAPPE E8b (U43) — die LAGE, aus der die Checkliste ihren Stand ableitet: Was der
    /// Lauf gerechnet und der Anwender gepflegt hat. Bericht und Ergebnisseite füllen sie
    /// aus ihren eigenen Daten (<see cref="AusBericht"/> bzw. die Seite aus ihrem Stand); die
    /// Punkte selbst entstehen EINMAL in <see cref="AnhangECheckliste.Punkte"/>.
    /// </summary>
    public sealed class ChecklistenLage
    {
        /// <summary>Mindestens ein Stand trägt im Szenario „Erwartet" einen Kapitalwert.</summary>
        public bool Gerechnet;

        /// <summary>Nicht monetäre Wirkungen sind erfasst — ETAPPE E17: mindestens eine Wirkung
        /// der Liste trägt eine Beschreibung.</summary>
        public bool NichtMonetaerErfasst;

        /// <summary>ETAPPE E17 (V‑G11): mindestens eine Wirkung ist nach DIN EN 17463 8.2
        /// beurteilt (Dauer und ein Wirkungsgrad) — die Punkte 2b und 3b stehen dann auf
        /// „erfüllt".</summary>
        public bool NichtMonetaerBeurteilt;

        /// <summary>Der Betrachtungszeitraum ist gegen die Nutzungsdauern abgeglichen.</summary>
        public bool ZeitraumBegruendet;

        /// <summary>Positionen ohne Nutzungsdauer sind gemeldet.</summary>
        public bool PositionenOhneNutzungsdauer;

        /// <summary>Die Sensitivität ist gerechnet.</summary>
        public bool SensitivitaetGerechnet;

        /// <summary>Ungünstig UND Günstig tragen für mindestens einen Stand eine
        /// Kapitalwertdifferenz (Zeile der Bandbreite mit beiden Werten) — eine Bandbreite
        /// nur aus Zeilen ohne Zahl ist keine Szenarioanalyse.</summary>
        public bool SzenarienGerechnet;

        /// <summary>
        /// ETAPPE E13 (E9b‑Q5 b): Ungünstig ODER Günstig trägt für mindestens einen Stand eine
        /// Zahl — nur ein Szenario gerechnet; Punkt 9 steht dann auf „teilweise".
        /// </summary>
        public bool EinSzenarioGerechnet;

        /// <summary>Es gibt einen Vorschlag zur Entscheidung.</summary>
        public bool VorschlagVorhanden;

        /// <summary>
        /// ETAPPE E9b (Konzept § 2.11.5, Pflege): der Ausweis „n von m Parametern
        /// szenariert" (<see cref="SzenarioAbdeckung.Satz"/>) — Punkt 9 (Szenarioanalyse)
        /// nennt ihn in seinem Stand; leer = nicht bekannt, dann bleibt der allgemeine Satz.
        /// </summary>
        public string Szenarioabdeckung = "";

        /// <summary>
        /// ETAPPE E15 (V‑G7): die Kurzfassung des gepflegten Risikos
        /// (<see cref="RisikoModul.Kurz"/>); leer = kein Risiko angesetzt. Punkt 6 nennt es.
        /// </summary>
        public string Risiko = "";

        /// <summary>Die Lage eines Berichtslaufs — aus seiner Bewertung und dem Parametersatz.</summary>
        public static ChecklistenLage AusBericht(IList<WirtschaftlichkeitErgebnis> alle,
                                                 WirtschaftlichkeitParameter p,
                                                 WirtschaftlichkeitBewertung bewertung)
        {
            var lage = new ChecklistenLage
            {
                Gerechnet = alle != null && alle.Any(e => e != null &&
                            e.Szenario == WirtschaftlichkeitSzenario.ERWARTET && e.Kapitalwert.HasValue),
                NichtMonetaerErfasst = p != null && !string.IsNullOrWhiteSpace(p.NichtMonetaer),
                // ETAPPE E15: das gepflegte Risiko (leer ohne Pflege).
                Risiko = RisikoModul.Kurz(p, BerichtTexte.Kultur)
            };
            // ETAPPE E17 (V‑G11): Trägt die Bewertung die Wirkungsliste, gilt sie — erfasst
            // heißt „eine Wirkung beschrieben", beurteilt „eine Wirkung nach 8.2 beurteilt".
            // Ohne Liste (Bewertung ohne Berichtslauf) bleibt der Freitext die Quelle.
            if (bewertung != null && bewertung.Wirkungen != null)
            {
                lage.NichtMonetaerErfasst = NichtMonetaereWirkungen.Benannt(bewertung.Wirkungen);
                lage.NichtMonetaerBeurteilt = NichtMonetaereWirkungen.Beurteilt(bewertung.Wirkungen);
            }
            if (bewertung != null)
            {
                NutzungsdauerHinweise nd = bewertung.Nutzungsdauer;
                lage.ZeitraumBegruendet = nd != null && !string.IsNullOrEmpty(nd.Zeitraumzeile);
                lage.PositionenOhneNutzungsdauer = nd != null && nd.Zeilen.Count > 0;
                lage.SensitivitaetGerechnet = bewertung.Sensitivitaet != null && bewertung.Sensitivitaet.Count > 0;
                lage.SzenarienGerechnet = bewertung.Bandbreite != null &&
                    bewertung.Bandbreite.Zeilen.Any(z => z != null && z.Worst.HasValue && z.Best.HasValue);
                lage.EinSzenarioGerechnet = bewertung.Bandbreite != null &&
                    bewertung.Bandbreite.Zeilen.Any(z => z != null && (z.Worst.HasValue || z.Best.HasValue));
                lage.VorschlagVorhanden = !string.IsNullOrEmpty(bewertung.Vorschlagstext);
                lage.Szenarioabdeckung = bewertung.Szenarioabdeckung ?? "";
            }
            return lage;
        }
    }

    /// <summary>
    /// ETAPPE E8b (U43; Konzept § 2.11.2 V‑G12, Register Q18) — die <b>Checkliste für den
    /// Bewertungsbericht</b> nach DIN EN 17463 Anhang E: 15 Punkte in den Gruppen
    /// Gegenstand, A Aufbau des Modells, B Berechnung, C Auswertung, D Berichterstattung.
    ///
    /// <para><b>Was sie trägt:</b> je Punkt die Anforderung (mit eigenen Worten — der
    /// Normtext steht nicht im Programm), die <b>Stelle im Bericht</b>, an der ein Prüfer den
    /// Nachweis findet, und den <b>Stand</b>: was EPOS zu dem Punkt liefert und was fehlt —
    /// aus der Lage des Laufs abgeleitet, nicht behauptet. Die <b>Beurteilung 1–5</b>
    /// (1 = sehr gut) vergibt der Prüfer; die Spalte bleibt dafür frei.</para>
    ///
    /// <para><b>Wo sie steht:</b> als Abschlussseite des Wortberichts
    /// (<see cref="AnhangEChecklisteBaustein"/>) und als letztes Blatt der Mappe
    /// (<see cref="SchreibeExcel"/>), beide nur mit dem Baustein „Wirtschaftlichkeit"; auf
    /// der Ergebnisseite hinter dem Knopf „Anhang-E-Checkliste…". Alle drei lesen dieselben
    /// Punkte.</para>
    /// </summary>
    public static class AnhangECheckliste
    {
        /// <summary>Zahl der Punkte der Norm (0.1, 0.2, 1, 2a, 2b, 3a, 3b, 4 bis 11).</summary>
        public const int ANZAHL = 15;

        /// <summary>Name des Blattes in der Mappe (höchstens 31 Zeichen).</summary>
        public static string Blattname { get { return MyResource.Resource.WIRT_AE_BLATT; } }

        /// <summary>
        /// Die Kapitel, auf die die Checkliste in ihrer Spalte „Stelle“ verweist, als
        /// <see cref="Berichtskapitel.Stellenschluessel"/>: Deckblatt, Projektbeschreibung, Komponenten,
        /// Ergebnisse, Vergleich, Wirtschaftlichkeit, Anhang. Führt eine Vorlage mit Anhang E eines davon
        /// nicht, meldet der Vorlagenprüfer „Anhang E ohne Stelle“.
        /// </summary>
        public static readonly IReadOnlyList<string> Kapitelbezuege = new[]
        {
            BerichtsKonfiguration.B_DECKBLATT, BerichtsKonfiguration.B_PROJEKT, BerichtsKonfiguration.B_KOMPONENTEN,
            BerichtsKonfiguration.B_ERGEBNISSE, BerichtsKonfiguration.B_VERGLEICH, BerichtsKonfiguration.B_WIRTSCHAFT,
            BerichtsKonfiguration.B_ANHANG,
        };

        /// <summary>Die 15 Punkte mit ihrem Stand in dieser Lage; die Stellen nennen die eigenen Überschriften der Kapitel.</summary>
        public static List<ChecklistenPunkt> Punkte(ChecklistenLage lage)
        {
            return Punkte(lage, null);
        }

        /// <summary>
        /// Die 15 Punkte mit ihrem Stand in dieser Lage. Die Spalte „Stelle“ nennt im Wortbericht die
        /// TATSÄCHLICHE Überschrift jedes Kapitels (Konzept Berichtsvorlagen 11 Nr. 3):
        /// <paramref name="kapitelstellen"/> je <see cref="Berichtskapitel.Stellenschluessel"/> — aus der
        /// gefüllten Vorlage (<see cref="WordKontext.Kapitelstellen"/>) oder vorab aus der gewählten
        /// (<see cref="BerichtCtrl.KapitelstellenDerVorlage"/>); <c>null</c> als Wert heißt „nicht im
        /// Bericht“, <c>null</c> als Verzeichnis die eigenen Überschriften aller Kapitel.
        /// </summary>
        public static List<ChecklistenPunkt> Punkte(ChecklistenLage lage, IReadOnlyDictionary<string, string> kapitelstellen)
        {
            lage = lage ?? new ChecklistenLage();
            IReadOnlyDictionary<string, string> st = kapitelstellen ?? Berichtskapitel.EigeneStellen(null, BerichtTexte.Englisch);
            string w = BerichtsKonfiguration.B_WIRTSCHAFT;
            string g0 = MyResource.Resource.WIRT_AE_GRUPPE_0, gA = MyResource.Resource.WIRT_AE_GRUPPE_A,
                   gB = MyResource.Resource.WIRT_AE_GRUPPE_B, gC = MyResource.Resource.WIRT_AE_GRUPPE_C,
                   gD = MyResource.Resource.WIRT_AE_GRUPPE_D;
            string ohneRechnung = MyResource.Resource.WIRT_AE_OHNE_RECHNUNG;

            var l = new List<ChecklistenPunkt>
            {
                Punkt("0.1", g0, MyResource.Resource.WIRT_AE_01_THEMA, MyResource.Resource.WIRT_AE_01_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_01_STELLE), null, st, BerichtsKonfiguration.B_DECKBLATT), ChecklistenStand.Erfuellt, MyResource.Resource.WIRT_AE_01_STAND),
                Punkt("0.2", g0, MyResource.Resource.WIRT_AE_02_THEMA, MyResource.Resource.WIRT_AE_02_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_02_STELLE), null, st, BerichtsKonfiguration.B_PROJEKT, BerichtsKonfiguration.B_KOMPONENTEN), ChecklistenStand.Teilweise, MyResource.Resource.WIRT_AE_02_STAND),
                Punkt("1", gA, MyResource.Resource.WIRT_AE_1_THEMA, MyResource.Resource.WIRT_AE_1_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_1_STELLE), nameof(MyResource.Resource.WIRT_AE_1_STELLE_WORT), st, w),
                      lage.Gerechnet ? ChecklistenStand.Erfuellt : ChecklistenStand.Offen,
                      lage.Gerechnet ? MyResource.Resource.WIRT_AE_1_STAND : ohneRechnung),
                Punkt("2a", gA, MyResource.Resource.WIRT_AE_2A_THEMA, MyResource.Resource.WIRT_AE_2A_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_2A_STELLE), null, st, BerichtsKonfiguration.B_ERGEBNISSE, BerichtsKonfiguration.B_VERGLEICH), ChecklistenStand.Erfuellt, MyResource.Resource.WIRT_AE_2A_STAND),
                Punkt("2b", gA, MyResource.Resource.WIRT_AE_2B_THEMA, MyResource.Resource.WIRT_AE_2B_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_2B_STELLE), nameof(MyResource.Resource.WIRT_AE_2B_STELLE_WORT), st, w), NmStand(lage), NmStandText(lage)),
                Punkt("3a", gA, MyResource.Resource.WIRT_AE_3A_THEMA, MyResource.Resource.WIRT_AE_3A_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_3A_STELLE), nameof(MyResource.Resource.WIRT_AE_3A_STELLE_WORT), st, w),
                      lage.Gerechnet ? ChecklistenStand.Erfuellt : ChecklistenStand.Offen,
                      lage.Gerechnet ? MyResource.Resource.WIRT_AE_3A_STAND : ohneRechnung),
                Punkt("3b", gA, MyResource.Resource.WIRT_AE_3B_THEMA, MyResource.Resource.WIRT_AE_3B_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_2B_STELLE), nameof(MyResource.Resource.WIRT_AE_2B_STELLE_WORT), st, w), NmStand(lage), NmStandText(lage)),
                // Die Zeitpunkte der Zahlungen zeigt erst die Mehrjahrestabelle eines Laufs.
                Punkt("4", gA, MyResource.Resource.WIRT_AE_4_THEMA, MyResource.Resource.WIRT_AE_4_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_4_STELLE), nameof(MyResource.Resource.WIRT_AE_4_STELLE_WORT), st, w),
                      !lage.Gerechnet ? ChecklistenStand.Offen
                      : lage.ZeitraumBegruendet && !lage.PositionenOhneNutzungsdauer
                          ? ChecklistenStand.Erfuellt : ChecklistenStand.Teilweise,
                      !lage.Gerechnet ? ohneRechnung
                      : lage.ZeitraumBegruendet && !lage.PositionenOhneNutzungsdauer
                          ? MyResource.Resource.WIRT_AE_4_ERFUELLT : MyResource.Resource.WIRT_AE_4_TEILWEISE),
                Punkt("5", gA, MyResource.Resource.WIRT_AE_5_THEMA, MyResource.Resource.WIRT_AE_5_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_5_STELLE), nameof(MyResource.Resource.WIRT_AE_5_STELLE_WORT), st, w), ChecklistenStand.Teilweise, MyResource.Resource.WIRT_AE_5_STAND),
                // ETAPPE E15 (V‑G7): Mit gepflegtem Risiko nennt der Stand, wie es angesetzt ist;
                // „teilweise" bleibt, weil die Degradation weiter nicht gerechnet wird (A5).
                Punkt("6", gA, MyResource.Resource.WIRT_AE_6_THEMA, MyResource.Resource.WIRT_AE_6_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_6_STELLE), nameof(MyResource.Resource.WIRT_AE_6_STELLE_WORT), st, w), ChecklistenStand.Teilweise,
                      string.IsNullOrEmpty(lage.Risiko)
                          ? MyResource.Resource.WIRT_AE_6_STAND
                          : string.Format(BerichtTexte.Kultur, MyResource.Resource.WIRT_AE_6_STAND_RISIKO, lage.Risiko)),
                Punkt("7", gB, MyResource.Resource.WIRT_AE_7_THEMA, MyResource.Resource.WIRT_AE_7_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_7_STELLE), nameof(MyResource.Resource.WIRT_AE_7_STELLE_WORT), st, w),
                      lage.Gerechnet ? ChecklistenStand.Erfuellt : ChecklistenStand.Offen,
                      lage.Gerechnet ? MyResource.Resource.WIRT_AE_7_STAND : ohneRechnung),
                Punkt("8", gB, MyResource.Resource.WIRT_AE_8_THEMA, MyResource.Resource.WIRT_AE_8_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_8_STELLE), nameof(MyResource.Resource.WIRT_AE_8_STELLE_WORT), st, w),
                      lage.SensitivitaetGerechnet ? ChecklistenStand.Teilweise : ChecklistenStand.Offen,
                      lage.SensitivitaetGerechnet ? MyResource.Resource.WIRT_AE_8_TEILWEISE : MyResource.Resource.WIRT_AE_8_OFFEN),
                Punkt("9", gB, MyResource.Resource.WIRT_AE_9_THEMA, MyResource.Resource.WIRT_AE_9_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_9_STELLE), nameof(MyResource.Resource.WIRT_AE_9_STELLE_WORT), st, w),
                      StandPunkt9(lage), StandTextPunkt9(lage)),
                Punkt("10", gC, MyResource.Resource.WIRT_AE_10_THEMA, MyResource.Resource.WIRT_AE_10_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_10_STELLE), nameof(MyResource.Resource.WIRT_AE_10_STELLE_WORT), st, w),
                      lage.VorschlagVorhanden && lage.SzenarienGerechnet ? ChecklistenStand.Erfuellt : ChecklistenStand.Offen,
                      lage.VorschlagVorhanden && lage.SzenarienGerechnet
                          ? MyResource.Resource.WIRT_AE_10_ERFUELLT : MyResource.Resource.WIRT_AE_10_OFFEN),
                Punkt("11", gD, MyResource.Resource.WIRT_AE_11_THEMA, MyResource.Resource.WIRT_AE_11_ANF,
                      Stelle(nameof(MyResource.Resource.WIRT_AE_11_STELLE), nameof(MyResource.Resource.WIRT_AE_11_STELLE_WORT), st, w, BerichtsKonfiguration.B_ANHANG), ChecklistenStand.Erfuellt, MyResource.Resource.WIRT_AE_11_STAND),
            };
            return l;
        }

        /// <summary>
        /// ETAPPE E13 (E9b‑Q5 b) — der Stand von Punkt 9 (Szenarioanalyse): „erfüllt", sobald
        /// Ungünstig UND Günstig mit Kapitalwert gerechnet sind — unabhängig davon, ob ein
        /// Parameter szenariert ist; „teilweise", wenn nur Erwartet oder nur eines der beiden
        /// Szenarien vorliegt; „offen" ohne Lauf.
        /// </summary>
        internal static ChecklistenStand StandPunkt9(ChecklistenLage lage)
        {
            if (lage.SzenarienGerechnet) return ChecklistenStand.Erfuellt;
            if (lage.Gerechnet || lage.EinSzenarioGerechnet) return ChecklistenStand.Teilweise;
            return ChecklistenStand.Offen;
        }

        /// <summary>Der Standtext von Punkt 9 — mit dem Ausweis „n von m Parametern
        /// szenariert" als Beleg, wo die Lage ihn kennt (ETAPPE E9b).</summary>
        private static string StandTextPunkt9(ChecklistenLage lage)
        {
            bool abdeckung = !string.IsNullOrEmpty(lage.Szenarioabdeckung);
            switch (StandPunkt9(lage))
            {
                case ChecklistenStand.Erfuellt:
                    return abdeckung ? string.Format(MyResource.Resource.WIRT_AE_9_ABDECKUNG, lage.Szenarioabdeckung)
                                     : MyResource.Resource.WIRT_AE_9_ERFUELLT;
                case ChecklistenStand.Teilweise:
                    return abdeckung ? string.Format(MyResource.Resource.WIRT_AE_9_TEILWEISE_ABDECKUNG, lage.Szenarioabdeckung)
                                     : MyResource.Resource.WIRT_AE_9_TEILWEISE;
                default:
                    return MyResource.Resource.WIRT_AE_9_OFFEN;
            }
        }

        /// <summary>
        /// Die Stelle eines Punkts: das Muster <paramref name="muster"/> („Wortbericht: {0} · Tabellenbericht:
        /// …“) mit dem Wortteil als {0}. Der Wortteil nennt die Überschriften der <paramref name="kapitel"/>
        /// im Bericht — in <paramref name="wort"/> eingesetzt, wenn es alle gibt; sonst die vorhandenen,
        /// durch Komma getrennt; ohne jedes „nicht im Bericht“. Ein Wortteil ohne {0} (ein Abschnitt im
        /// Kapitel) steht, solange sein Kapitel im Bericht ist. Das Deckblatt steht ohne Anführungszeichen —
        /// es hat keine Überschrift.
        /// </summary>
        private static string Stelle(string muster, string wort, IReadOnlyDictionary<string, string> stellen,
                                     params string[] kapitel)
        {
            var namen = new List<string>();
            foreach (string k in kapitel)
            {
                if (!stellen.TryGetValue(k, out string ueberschrift) || string.IsNullOrWhiteSpace(ueberschrift)) continue;
                namen.Add(k == BerichtsKonfiguration.B_DECKBLATT
                    ? ueberschrift
                    : Format(MyResource.Resource.WIRT_AE_STELLE_KAPITEL, ueberschrift));
            }

            string format = wort == null ? null : Ressource(wort);
            string wortteil;
            if (namen.Count == 0) wortteil = MyResource.Resource.WIRT_AE_STELLE_NICHT_IM_BERICHT;
            else if (format != null && namen.Count == kapitel.Length) wortteil = Format(format, namen.ToArray());
            else if (format != null && !format.Contains("{0}", StringComparison.Ordinal)) wortteil = format;
            else wortteil = string.Join(", ", namen);
            return Format(Ressource(muster), wortteil);
        }

        private static string Ressource(string schluessel)
        {
            return MyResource.Resource.ResourceManager.GetString(schluessel, MyResource.Resource.Culture) ?? schluessel;
        }

        private static string Format(string muster, params object[] werte)
        {
            try { return string.Format(BerichtTexte.Kultur, muster, werte); }
            catch (FormatException) { return muster; }
        }

        private static ChecklistenPunkt Punkt(string nummer, string gruppe, string thema, string anforderung,
                                              string stelle, ChecklistenStand stand, string standText)
        {
            return new ChecklistenPunkt
            {
                Nummer = nummer, Gruppe = gruppe, Thema = thema, Anforderung = anforderung,
                Stelle = stelle, Stand = stand, StandText = standText
            };
        }

        /// <summary>
        /// ETAPPE E17 (V‑G11): der Stand der Punkte 2b und 3b — „erfüllt", sobald mindestens
        /// eine Wirkung nach 8.2 beurteilt ist, „teilweise", wenn Wirkungen nur beschrieben
        /// sind, sonst „offen".
        /// </summary>
        private static ChecklistenStand NmStand(ChecklistenLage lage)
            => lage.NichtMonetaerErfasst && lage.NichtMonetaerBeurteilt ? ChecklistenStand.Erfuellt
             : lage.NichtMonetaerErfasst ? ChecklistenStand.Teilweise
             : ChecklistenStand.Offen;

        private static string NmStandText(ChecklistenLage lage)
            => lage.NichtMonetaerErfasst && lage.NichtMonetaerBeurteilt ? MyResource.Resource.WIRT_AE_NM_ERFUELLT
             : lage.NichtMonetaerErfasst ? MyResource.Resource.WIRT_AE_NM_TEILWEISE
             : MyResource.Resource.WIRT_AE_NM_OFFEN;

        /// <summary>Die Punkte eines Berichtslaufs — Quelle wie im Baustein „Wirtschaftlichkeit":
        /// die Ergebnisse DIESES Laufs, ersatzweise der gespeicherte Stand.</summary>
        public static List<ChecklistenPunkt> AusBericht(BerichtsDaten daten)
        {
            return AusBericht(daten, null);
        }

        /// <summary>
        /// Die Punkte eines Berichtslaufs mit den Stellen der Kapitel in DIESEM Bericht
        /// (<see cref="Punkte(ChecklistenLage, IReadOnlyDictionary{string, string})"/>); <c>null</c> = die
        /// eigenen Überschriften.
        /// </summary>
        public static List<ChecklistenPunkt> AusBericht(BerichtsDaten daten, IReadOnlyDictionary<string, string> kapitelstellen)
        {
            // BV-E3 (Konzept Berichtsvorlagen 5.1): aus dem Wertesatz des Laufs — Ergebnisse, Parameter,
            // Bewertung und Wirkungsliste hat der Sammler über dieselben Aufrufe ermittelt.
            WirtschaftsBerichtswerte w = WirtschaftsBerichtswerte.Von(daten);
            List<WirtschaftlichkeitErgebnis> alle = w.Ergebnisse;
            WirtschaftlichkeitParameter p = w.Parameter;
            WirtschaftlichkeitBewertung bewertung = daten.Bewertung;
            if (bewertung == null && alle.Count > 0)
            {
                try { bewertung = w.Bewertung; }
                catch { bewertung = null; }   // ohne Bewertung bleiben die Punkte „offen"
            }
            ChecklistenLage lage = ChecklistenLage.AusBericht(alle, p, bewertung);
            if (bewertung == null || bewertung.Wirkungen == null)
            {
                // ETAPPE E17: auch ohne Bewertung zählt die Wirkungsliste, nicht der Freitext.
                List<ProjektWirkung> wirkungen = w.Wirkungen;
                lage.NichtMonetaerErfasst = NichtMonetaereWirkungen.Benannt(wirkungen);
                lage.NichtMonetaerBeurteilt = NichtMonetaereWirkungen.Beurteilt(wirkungen);
            }
            return Punkte(lage, kapitelstellen);
        }

        // =====================================================================
        //  Tabellenbericht — das letzte Blatt der Mappe
        // =====================================================================

        /// <summary>Die Spalten: Nr., Thema, Anforderung, Stelle im Bericht, Stand, Note.</summary>
        internal const int SPALTE_NOTE = 6;

        /// <summary>
        /// Schreibt die Checkliste als eigenes Blatt ans Ende der Mappe. Die Spalte
        /// „Beurteilung 1–5" nimmt nur ganze Zahlen von 1 bis 5 an (Gültigkeitsprüfung).
        /// </summary>
        internal static void SchreibeExcel(XLWorkbook wb, List<ChecklistenPunkt> punkte)
        {
            if (wb == null || punkte == null || punkte.Count == 0) return;
            IXLWorksheet ws = wb.Worksheets.Add(Blattname);

            int r = 1;
            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_AE_TITEL;
            ws.Cell(r, 1).Style.Font.Bold = true;
            ws.Cell(r, 1).Style.Font.FontSize = 14;
            r++;
            ws.Cell(r, 1).Value = MyResource.Resource.WIRT_AE_HINWEIS;
            ws.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#696969");
            r += 2;

            string[] kopf =
            {
                MyResource.Resource.WIRT_AE_SP_NR, MyResource.Resource.WIRT_AE_SP_THEMA,
                MyResource.Resource.WIRT_AE_SP_ANFORDERUNG, MyResource.Resource.WIRT_AE_SP_STELLE,
                MyResource.Resource.WIRT_AE_SP_STAND, MyResource.Resource.WIRT_AE_SP_NOTE
            };
            for (int i = 0; i < kopf.Length; i++) ws.Cell(r, 1 + i).Value = kopf[i];
            ws.Range(r, 1, r, kopf.Length).Style.Font.Bold = true;
            ws.Range(r, 1, r, kopf.Length).Style.Fill.BackgroundColor = ExcelBerichtGenerator.KOPF;
            int ersteZeile = r + 1;
            r++;

            string gruppe = null;
            foreach (ChecklistenPunkt pkt in punkte)
            {
                if (!string.Equals(gruppe, pkt.Gruppe, StringComparison.Ordinal))
                {
                    gruppe = pkt.Gruppe;
                    ws.Cell(r, 1).Value = gruppe;
                    ws.Cell(r, 1).Style.Font.Bold = true;
                    ws.Range(r, 1, r, kopf.Length).Style.Fill.BackgroundColor = ExcelBerichtGenerator.GRUPPE;
                    r++;
                }
                ws.Cell(r, 1).Value = pkt.Nummer;
                ws.Cell(r, 2).Value = pkt.Thema;
                ws.Cell(r, 3).Value = pkt.Anforderung;
                ws.Cell(r, 4).Value = pkt.Stelle;
                ws.Cell(r, 5).Value = pkt.StandZeile;
                ws.Range(r, 1, r, kopf.Length).Style.Alignment.WrapText = true;
                ws.Range(r, 1, r, kopf.Length).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                r++;
            }

            // Die Beurteilung trägt der Prüfer ein — nur ganze Zahlen von 1 bis 5.
            IXLDataValidation note = ws.Range(ersteZeile, SPALTE_NOTE, r - 1, SPALTE_NOTE).CreateDataValidation();
            note.WholeNumber.Between(1, 5);
            note.ErrorMessage = MyResource.Resource.WIRT_AE_NOTE_FEHLER;

            ws.Column(1).Width = 8;
            ws.Column(2).Width = 28;
            ws.Column(3).Width = 55;
            ws.Column(4).Width = 45;
            ws.Column(5).Width = 55;
            ws.Column(6).Width = 12;
            ws.SheetView.FreezeRows(ersteZeile - 1);
        }
    }

    /// <summary>
    /// ETAPPE E8b (U43) — die Anhang-E-Checkliste als <b>Abschlussseite des Wortberichts</b>.
    /// Sie hängt am Baustein „Wirtschaftlichkeit" (derselbe Schlüssel): ohne
    /// Wirtschaftlichkeit keine Checkliste — ihre Punkte verweisen auf deren Kapitel.
    /// </summary>
    public class AnhangEChecklisteBaustein : IBerichtsBaustein
    {
        public string Schluessel { get { return BerichtsKonfiguration.B_WIRTSCHAFT; } }
        public string Titel { get { return MyResource.Resource.WIRT_AE_TITEL; } }

        public void SchreibeWord(WordKontext k, BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            // Die Stellen nennen die Überschriften der Kapitel in DIESEM Bericht (Vorlagenweg); der
            // bisherige Weg führt keine — dann die eigenen Überschriften.
            List<ChecklistenPunkt> punkte = AnhangECheckliste.AusBericht(daten, k.Kapitelstellen);
            if (punkte.Count == 0) return;

            k.Seitenumbruch();
            k.MitStilRoh("Heading1", MyResource.Resource.WIRT_AE_TITEL);
            k.HinweisRoh(MyResource.Resource.WIRT_AE_HINWEIS);

            // Nr. · Thema · Anforderung · Stelle im Bericht · Stand · Beurteilung — BV-E5: dieselbe Tafel wie
            // {{tabelle.anhang_e.checkliste}} (Berichtstabellen.AnhangE).
            k.Fuege(WordTabellenschreiber.Direkt(k, Berichtstabellen.AnhangE(punkte, k.Kultur)));
        }
    }
}
