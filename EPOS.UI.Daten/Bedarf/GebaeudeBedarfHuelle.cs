using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE des Bedarfsdialogs EINES Gebäudes (<c>GebaeudeBedarfDialog</c>,
    /// Anwenderwunsch W9‑E‑2) — seit Stufe G1 eine eigene Datei in <c>EPOS.UI.Daten</c>
    /// (Umsetzungskonzept Gebäudesimulation 2.8).
    ///
    /// <para><b>Gerechnet wird im Kern</b> (<c>GebaeudeBedarfCtrl</c>), gezeichnet auch
    /// (<c>ChartRenderer.GanglinieNormiertModell</c>, <c>ChartRenderer.RaumtemperaturModell</c>)
    /// — die Komponente bekommt Zahlen und Zeichenmodelle. Das DTO trägt den Rechenweg (Ausweis
    /// „Tagesbilanz (Bestandsweg)" nach ADR-006), die Kennzahlen des VDI-Wegs und den
    /// <b>Vergleich alt/neu</b>: zwei Aufrufe desselben Controllers, der zweite mit
    /// <c>modellErzwungen</c> auf den jeweils anderen Weg (Umsetzungskonzept 2.7) — bis Stufe GA.</para>
    /// </summary>
    internal static class GebaeudeBedarfHuelle
    {
        /// <summary>
        /// Der Parametersatz des Bedarfsdialogs zu EINER Projektzeile — <c>null</c>, wenn
        /// es dafür keine Zahl gibt: kein Projekt (Katalogverwaltung), keine Klimaregion
        /// oder eine eben erst aufgenommene Zeile ohne Projektkopie. Der Dialog MELDET das.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            GebaeudeProjektZeile zeile, int projektId)
            => Gaben(zeile, projektId, out _);

        /// <summary>
        /// Dasselbe mit dem benannten Grund, wenn es keine Zahl gibt (Stufe G6a): <paramref name="befund"/>
        /// trägt dann den Fehler, mit dem die Fassade das Gebäude ablehnt (etwa mehrere Zonen) —
        /// <c>null</c>, wenn es gar nichts zu rechnen gab; dann gilt die allgemeine Meldung des Dialogs.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            GebaeudeProjektZeile zeile, int projektId, out string befund)
        {
            befund = null;
            if (zeile == null || projektId <= 0) return null;

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(projektId);

            GebaeudeBedarfErgebnis ergebnis =
                GebaeudeBedarfCtrl.Rechnen(projektId, projekt.m_ID_Klimaregion, zeile.IdZ);
            if (!ergebnis.Erfolgreich)
            {
                // Eine eben aufgenommene Zeile (etwa aus dem Gebäudeimport) hat noch keine Projektkopie:
                // Der Grund ist dann das fehlende OK, nicht Projekt oder Klimaregion.
                befund = ergebnis.Befund
                         ?? (zeile.IdZ >= GebaeudeHuelle.STARTINDEX ? MyResource.Resource.GEB_MSG_BEDARF_UNGESPEICHERT : null);
                return null;
            }

            // Der Vergleich alt/neu: derselbe Controller, der andere Rechenweg. Liefert er
            // nichts (etwa ein benannt abgelehntes Gebaeude auf dem VDI-Weg), bleibt die
            // Vergleichstabelle weg - die Meldung steht im Protokoll des Aufrufs.
            string anderer = ergebnis.Modell == DbWerte.GEBAEUDE_MODELL_VDI6007
                ? DbWerte.GEBAEUDE_MODELL_TAGESBILANZ
                : DbWerte.GEBAEUDE_MODELL_VDI6007;
            GebaeudeBedarfErgebnis gegen =
                GebaeudeBedarfCtrl.Rechnen(projektId, projekt.m_ID_Klimaregion, zeile.IdZ, anderer);

            GebaeudeBedarfDaten daten = Daten(ergebnis, gegen.Erfolgreich ? Daten(gegen, null) : null);

            // Das Bild "Raumtemperatur" gibt es nur auf dem VDI-Weg (Konzept 8.2).
            Func<Zeichenmodell> raumbild = ergebnis.RaumtemperaturC != null
                ? () => Raumtemperaturmodell(ergebnis)
                : null;

            // Das Bild der Kaeltelast (Stufe KU1) - nur mit Kuehlreihe; auf dem Bestandsweg gibt
            // es keine Reihe, der Abschnitt steht dort mit 0 und Hinweis, und ein Gebaeude ohne
            // wirksame Kuehlung laeuft frei und hat keine Kuehlreihe (E32).
            Func<bool, Zeichenmodell> kaeltebild = ergebnis.KuehlbedarfKwh != null
                ? sortiert => Kaeltemodell(ergebnis, sortiert)
                : null;

            // Das Bild „Vorlauf und Rücklauf" (Anlagenkopplung AK1, 9.4) - nur für ein gekoppelt
            // gerechnetes Gebäude; Stunden ohne Heizbetrieb sind Lücken der Linie.
            Func<Zeichenmodell> vorlaufbild = ergebnis.Gekoppelt && ergebnis.VorlaufC != null
                ? () => Vorlaufmodell(ergebnis)
                : null;

            // E37: dasselbe Bild für die Kälteseite - Stunden ohne Kühlbetrieb sind Lücken.
            Func<Zeichenmodell> kuehlvorlaufbild = ergebnis.KuehlGekoppelt && ergebnis.KuehlVorlaufC != null
                ? () => Kuehlvorlaufmodell(ergebnis)
                : null;

            // Stufe G6b (Mehrzonenkonzept 2.8): Waermelast und Raumtemperatur je Zone - nur ab zwei
            // Zonen; eine unbeheizte Zone hat keine Waermelast (der Dialog nennt den Grund).
            Func<int, bool, Zeichenmodell> zonenbild = null;
            Func<int, Zeichenmodell> zonenraumbild = null;
            if (ergebnis.Zonen.Count >= 2)
            {
                zonenbild = (k, sortiert) => k >= 0 && k < ergebnis.Zonen.Count && ergebnis.Zonen[k].HeizlastKw != null
                    ? Lastmodell(ergebnis.Zonen[k].HeizlastKw, sortiert) : null;
                zonenraumbild = k => k >= 0 && k < ergebnis.Zonen.Count ? Raumtemperaturmodell(ergebnis.Zonen[k]) : null;
            }

            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["BildauftragZone"] = zonenbild,
                ["BildauftragRaumtemperaturZone"] = zonenraumbild,
                ["Bildauftrag"] = new Func<bool, Zeichenmodell>(
                    sortiert => Bedarfsmodell(ergebnis, sortiert)),
                ["BildauftragRaumtemperatur"] = raumbild,
                ["BildauftragKaelte"] = kaeltebild,
                ["BildauftragVorlauf"] = vorlaufbild,
                ["BildtextVorlauf"] = Text_("GEBB_BILD_VORLAUF_RUECKLAUF", "Vorlauf und Rücklauf"),
                ["KachelVorlaufRuecklauf"] = Text_("GEBB_KACHEL_VORLAUF_RUECKLAUF", "Vorlauf / Rücklauf"),
                ["KachelBegrenzt"] = Text_("GEBB_KACHEL_BEGRENZT", "Stunden mit begrenzter Übergabe"),
                ["QuelleBegrenzt"] = Text_("GEBB_KACHEL_BEGRENZT_QUELLE",
                                           "Stunden, in denen die Übergabe weniger lieferte, als der Sollwert verlangte"),
                ["HinweisVorlaufLuecken"] = Text_("GEBB_HRL_VORLAUF_LUECKEN",
                                                  "Stunden ohne Heizbetrieb bleiben im Bild leer — dort gibt es keinen Vorlauf."),
                ["BildauftragKuehlvorlauf"] = kuehlvorlaufbild,
                ["BildtextKuehlvorlauf"] = Text_("GEBB_BILD_KUEHLVORLAUF_RUECKLAUF", "Kühlvorlauf und Kühlrücklauf"),
                ["KachelKuehlvorlauf"] = Text_("GEBB_KACHEL_KUEHLVORLAUF_RUECKLAUF", "Kühlvorlauf / Kühlrücklauf"),
                ["KachelKuehlBegrenzt"] = Text_("GEBB_KACHEL_KUEHL_BEGRENZT", "Stunden mit begrenzter Kühlübergabe"),
                ["HinweisKuehlvorlaufLuecken"] = Text_("GEBB_HRL_KUEHLVORLAUF_LUECKEN",
                                                       "Stunden ohne Kühlbetrieb bleiben im Bild leer; der Kaltwasser-Vorlauf ist fest, der Rücklauf gehört zur gelieferten Kühlleistung."),
                ["GruppeKaelte"] = Text_("GEBB_GRP_KAELTE", "Kältebedarf"),
                ["LabelKaeltelastMax"] = Text_("GEBB_LBL_KAELTELAST_MAX", "max. Kältelast"),
                ["LabelHeizenUndKuehlen"] = Text_("GEBB_LBL_HEIZEN_UND_KUEHLEN", "Stunden mit Heizen und Kühlen:"),
                ["BildtextKaelte"] = Text_("CHART_TITEL_KAELTELAST_JAHRESGANGLINIE", "Kältelast Jahresganglinie"),
                ["SpalteHeizung"] = MyResource.Resource.KANAL_HEIZUNG_ANZEIGE,
                ["SpalteKuehlung"] = MyResource.Resource.KANAL_KUEHLUNG_ANZEIGE,
                ["BildtextRaumtemperatur"] = Text_("GEBB_BILD_RAUMTEMPERATUR", "Raumtemperatur"),
                ["GruppeVergleich"] = Text_("GEBB_GRP_VERGLEICH", "Vergleich der Rechenwege"),
                ["SpalteKennzahl"] = Text_("GEBB_SP_KENNZAHL", "Kennzahl"),
                ["SpalteTagesbilanz"] = Text_("GEBB_SP_TAGESBILANZ", "Tagesbilanz"),
                ["SpalteVdi6007"] = Text_("GEBB_SP_VDI6007", "VDI 6007"),
                ["SpalteAbweichung"] = Text_("GEBB_SP_ABWEICHUNG", "Abweichung"),
                ["LabelSpitzeStunde"] = Text_("GEBB_LBL_SPITZE_STUNDE", "Spitzenlast (Stunde):"),
                ["LabelSpitzeTagesmittel"] = Text_("GEBB_LBL_SPITZE_TAGESMITTEL", "Spitzenlast (Tagesmittel):"),
                ["LabelSpitzeQuantil95"] = Text_("GEBB_LBL_SPITZE_QUANTIL95", "95-%-Wert der Stundenlast:"),
                ["LabelKuehlbedarf"] = Text_("GEBB_LBL_KUEHLBEDARF", "Kühlbedarf:"),
                ["LabelKuehlstunden"] = Text_("GEBB_LBL_KUEHLSTUNDEN", "Stunden mit Kühlbedarf:"),
                ["LabelMittlereRaumtemperatur"] =
                    Text_("GEBB_LBL_MITTLERE_RAUMTEMPERATUR", "mittlere Raumtemperatur (Nutzungszeit):"),
                ["LabelUeberhitzung"] = Text_("GEBB_LBL_UEBERHITZUNG", "Überhitzungsstunden:"),
                ["LabelSommerlueftung"] = Text_("GEBB_LBL_SOMMERLUEFTUNG", "Stunden mit Sommerlüftung:"),
                ["EinheitStundenZahl"] = Text_("GEBB_EINHEIT_H", "h"),
                ["FarbeSetzen"] = new Func<Farbrolle, Farbe, Task>(FarbeSetzen),
                ["FarbeZuruecksetzen"] = new Func<Farbrolle, Task>(FarbeZuruecksetzen),

                // Die Anzeigeeinheit (Entscheid W8-O-5): dieselbe gemerkte Wahl wie im
                // Bedarfsprofil- und im Bedarfsergebnisdialog.
                ["Einheit"] = BedarfEinheitWahl.Lies(),
                ["EinheitGewaehlt"] = new Action<Energieeinheit>(BedarfEinheitWahl.Schreib),

                ["TitelText"] = Text_("GEBB_TITEL", "Wärmebedarf Gebäude"),
                ["GruppeKennzahlen"] = Text_("GEBB_GRP_KENNZAHLEN", "Kennzahlen"),
                ["GruppeMonate"] = Text_("BERG_GRP_MONAT", "monatlicher Verlauf:"),
                ["LabelHeizwaerme"] = Text_("GEBB_LBL_HEIZWAERME", "Wärmebedarf Heizung:"),
                ["LabelMaxLast"] = Text_("SIMERG_LBL_MAX_WAERMELAST", "max. Wärmelast"),
                ["LabelVollbenutzung"] =
                    Text_("GEBB_LBL_VOLLBENUTZUNG", "Vollbenutzungsstunden:"),
                ["LabelRechenweg"] = Text_("GEB_LBL_RECHENWEG", "Rechenweg:"),
                ["LabelSortiert"] = Text_("SIM_CHK_SORTIERT", "sortiert"),
                ["LabelEinheit"] = Text_("ALLG_LBL_EINHEIT", "Einheit:"),
                ["EinheitStunden"] = Text_("GEBB_EINHEIT_STUNDEN", "h/a"),
                ["Bildtext"] = Text_("CHART_TITEL_WAERMELAST_JAHRESGANGLINIE",
                                     "Wärmelast Jahresganglinie"),
                ["Monatsnamen"] = Monatsnamen(),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["HilfeSchluessel"] = "Form_Gebaeude.btn_Help"
            };
        }

        /// <summary>
        /// Das DTO eines Ergebnisses; <paramref name="vergleich"/> ist der andere Rechenweg
        /// (<c>null</c> = keiner). Energiemengen bleiben in MWh, umgerechnet wird an der
        /// Anzeigekante.
        /// </summary>
        private static GebaeudeBedarfDaten Daten(GebaeudeBedarfErgebnis ergebnis, GebaeudeBedarfDaten vergleich)
        {
            var monate = new double[12];
            for (int m = 0; m < 12 && m < ergebnis.MonatswerteMwh.Length; m++)
                monate[m] = ergebnis.MonatswerteMwh[m];

            return new GebaeudeBedarfDaten
            {
                Name = ergebnis.Name,
                HeizwaermeMwh = ergebnis.HeizwaermeMwh,
                MaxLastKw = ergebnis.MaxLastKw,
                VollbenutzungsstundenH = ergebnis.VollbenutzungsstundenH,
                MonatswerteMwh = monate,

                Modelltext = Rechenweg(ergebnis),
                IstVdi6007 = ergebnis.Modell == DbWerte.GEBAEUDE_MODELL_VDI6007,

                // Anlagenkopplung AK1 (9.4): der Heizkreis - nur gekoppelt, aus demselben Ergebnis.
                IstGekoppelt = ergebnis.Gekoppelt,
                VorlaufMittelC = ergebnis.Gekoppelt ? ergebnis.VorlaufMittelC : null,
                RuecklaufMittelC = ergebnis.Gekoppelt ? ergebnis.RuecklaufMittelC : null,
                UebergabeBegrenztStundenH = ergebnis.Gekoppelt ? ergebnis.UebergabeBegrenztStundenH : null,
                Heizkreiszeile = ergebnis.Gekoppelt ? Heizkreiszeile(ergebnis) : "",
                // E37: der Kaeltekreis - nur kuehlgekoppelt, aus demselben Ergebnis.
                IstKuehlgekoppelt = ergebnis.KuehlGekoppelt,
                KuehlVorlaufMittelC = ergebnis.KuehlGekoppelt ? ergebnis.KuehlVorlaufMittelC : null,
                KuehlRuecklaufMittelC = ergebnis.KuehlGekoppelt ? ergebnis.KuehlRuecklaufMittelC : null,
                KuehlUebergabeBegrenztStundenH = ergebnis.KuehlGekoppelt ? ergebnis.KuehlUebergabeBegrenztStundenH : null,
                Kuehlkreiszeile = ergebnis.KuehlGekoppelt ? Kuehlkreiszeile(ergebnis) : "",
                KuehlBegrenztzeile = ergebnis.KuehlGekoppelt ? KuehlBegrenztzeile(ergebnis) : "",
                SpitzeTagesmittelKw = ergebnis.SpitzeTagesmittelKw,
                SpitzeQuantil95Kw = ergebnis.SpitzeQuantil95Kw,
                // Stufe KU1 (F-K18): Auf dem Bestandsweg bucht der Lauf Kaeltebedarf 0 mit
                // Hinweis - die Auskunft zeigt dieselbe 0, nicht „—" (bis Stufe GA).
                KuehlenergieMwh = ergebnis.KaelteBestandsweg ? 0.0 : ergebnis.KuehlenergieMwh,
                KuehlstundenH = ergebnis.KaelteBestandsweg ? 0 : ergebnis.KuehlstundenH,
                MittlereRaumtemperaturC = ergebnis.MittlereRaumtemperaturC,
                UeberhitzungsstundenH = ergebnis.UeberhitzungsstundenH,
                SommerlueftungsstundenH = ergebnis.SommerlueftungsstundenH,
                Vergleich = vergleich,

                // Stufe KU1 (Kuehlkonzept 8.4): der Abschnitt „Kaeltebedarf" - fuer jedes Gebaeude
                // auf dem VDI-Weg, auch ungekuehlt: Dort stehen die Kaeltezahlen als „—" (K18, E32)
                // und die Ueberhitzungsstunden des freien Laufs.
                KaelteAbschnitt = ergebnis.Modell == DbWerte.GEBAEUDE_MODELL_VDI6007 || ergebnis.KaelteBestandsweg,
                KaeltelastMaxKw = ergebnis.KaelteBestandsweg ? 0.0 : ergebnis.KaeltelastMaxKw,
                VollbenutzungsstundenKaelteH = ergebnis.VollbenutzungsstundenKaelteH,
                StundenHeizenUndKuehlenH = ergebnis.StundenHeizenUndKuehlen,
                KuehlMonatswerteMwh = ergebnis.KuehlMonatswerteMwh != null
                    ? (IReadOnlyList<double>)(double[])ergebnis.KuehlMonatswerteMwh.Clone()
                    : ergebnis.KaelteBestandsweg ? new double[12] : new List<double>(),
                KaelteHerleitung = Kaelteherleitung(ergebnis),

                // Stufe G6b (A2): eine Zeile je Zone, auch unbeheizt - dort ohne Energie.
                Zonen = ergebnis.Zonen.ConvertAll(z => new GebaeudeBedarfZoneDaten
                {
                    Name = z.Name,
                    IstBeheizt = z.IstBeheizt,
                    HeizwaermeMwh = z.HeizwaermeMwh,
                    MaxLastKw = z.MaxLastKw,
                    MittlereRaumtemperaturC = z.MittlereRaumtemperaturC,
                    UeberhitzungsstundenH = z.UeberhitzungsstundenH
                })
            };
        }

        /// <summary>
        /// Der Rechenweg samt Ausweis der Kopplung (Anlagenkopplung 9.4): auf dem VDI-Weg mit
        /// wirksamer Kopplung einer Seite (Heiz- oder Kälteseite, E37) „VDI 6007, gekoppelt (AK1)",
        /// sonst der Rechenweg allein.
        /// </summary>
        internal static string Rechenweg(GebaeudeBedarfErgebnis e)
        {
            string text = GebaeudeHuelle.Rechenwegtext(e.Modell, vorgabe: false);
            return e.Gekoppelt || e.KuehlGekoppelt ? text + ", " + Text_("GEB_RECHENWEG_GEKOPPELT", "gekoppelt (AK1)") : text;
        }

        /// <summary>Die Zeile unter der Kühlvorlaufkachel: Kühlübergabeart, Auslegungspunkt und „sensibel" (K5).</summary>
        internal static string Kuehlkreiszeile(GebaeudeBedarfErgebnis e)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            return string.Format(k, Text_("GEBB_KACHEL_KUEHLVORLAUF_QUELLE",
                                          "Mittel der Stunden mit Kühlbetrieb — {0}, Auslegung {1}/{2} °C, sensibel"),
                                 Waermeuebergabevorgaben.KuehlAnzeigename(e.KuehlUebergabeArt),
                                 e.KuehlAuslegungVorlaufC.HasValue ? e.KuehlAuslegungVorlaufC.Value.ToString("N0", k) : "—",
                                 e.KuehlAuslegungRuecklaufC.HasValue ? e.KuehlAuslegungRuecklaufC.Value.ToString("N0", k) : "—");
        }

        /// <summary>Die Zeile unter der Kachel der begrenzten Stunden: davon an der Vorlaufgrenze, einer Vorgabe.</summary>
        internal static string KuehlBegrenztzeile(GebaeudeBedarfErgebnis e)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            return string.Format(k, Text_("GEBB_KACHEL_KUEHL_BEGRENZT_QUELLE",
                                          "Stunden, in denen die Kühlübergabe weniger lieferte, als der Kühlsollwert verlangte — davon {0} h an der Vorlaufgrenze (eine Vorgabe, keine Taupunktgrenze). Keine Überhitzungsstunden."),
                                 (e.KuehlVorlaufgrenzeStundenH ?? 0.0).ToString("N0", k));
        }

        /// <summary>
        /// Das Bild „Kühlvorlauf und Kühlrücklauf" (E37): DASSELBE Bild wie auf der Heizseite
        /// (<c>ChartRenderer.VorlaufRuecklaufModell</c>) mit den Reihen der Kälteseite. Der
        /// Kaltwasser-Vorlauf ist fest; Stunden ohne Kühlbetrieb (Kältebedarf 0) werden Lücken — das
        /// Bild zeigt, wann gekühlt wird, und erfindet keinen Rücklauf.
        /// </summary>
        private static Zeichenmodell Kuehlvorlaufmodell(GebaeudeBedarfErgebnis ergebnis)
        {
            double[] vorlauf = (double[])ergebnis.KuehlVorlaufC.Clone();
            double[] ruecklauf = ergebnis.KuehlRuecklaufC != null ? (double[])ergebnis.KuehlRuecklaufC.Clone() : null;
            double[] bedarf = ergebnis.KuehlbedarfKwh;
            for (int h = 0; h < vorlauf.Length; h++)
            {
                if (bedarf != null && h < bedarf.Length && bedarf[h] > 0.0) continue;
                vorlauf[h] = double.NaN;
                if (ruecklauf != null && h < ruecklauf.Length) ruecklauf[h] = double.NaN;
            }
            return ChartRenderer.VorlaufRuecklaufModell(
                Text_("GEBB_BILD_KUEHLVORLAUF_RUECKLAUF", "Kühlvorlauf und Kühlrücklauf"),
                vorlauf, ruecklauf,
                ergebnis.KuehlAuslegungVorlaufC, ergebnis.KuehlAuslegungRuecklaufC,
                new ChartRenderer.VorlaufRuecklaufnamen
                {
                    Vorlauf = Text_("GEBB_REIHE_KUEHLVORLAUF", "Kühlvorlauf"),
                    Ruecklauf = Text_("GEBB_REIHE_KUEHLRUECKLAUF", "Kühlrücklauf"),
                    AuslegungVorlauf = Text_("GEBB_REIHE_KUEHL_AUSLEGUNG_VORLAUF", "Auslegung Kühlvorlauf"),
                    AuslegungRuecklauf = Text_("GEBB_REIHE_KUEHL_AUSLEGUNG_RUECKLAUF", "Auslegung Kühlrücklauf"),
                });
        }

        /// <summary>Die Zeile unter der Vorlaufkachel: Übergabeart und der Auslegungspunkt, mit dem gerechnet wurde.</summary>
        internal static string Heizkreiszeile(GebaeudeBedarfErgebnis e)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            return string.Format(k, Text_("GEBB_KACHEL_VORLAUF_QUELLE",
                                          "Mittel der Stunden mit Heizbetrieb — {0}, Auslegung {1}/{2} °C"),
                                 Waermeuebergabevorgaben.Anzeigename(e.UebergabeArt),
                                 e.AuslegungVorlaufC.HasValue ? e.AuslegungVorlaufC.Value.ToString("N0", k) : "—",
                                 e.AuslegungRuecklaufC.HasValue ? e.AuslegungRuecklaufC.Value.ToString("N0", k) : "—");
        }

        /// <summary>
        /// Das Bild „Vorlauf und Rücklauf" (Anlagenkopplung AK1, 9.4): gefahrener Vorlauf und
        /// Rücklauf mit dem Auslegungspunkt — gezeichnet im Kern (<c>ChartRenderer.VorlaufRuecklaufModell</c>).
        /// </summary>
        private static Zeichenmodell Vorlaufmodell(GebaeudeBedarfErgebnis ergebnis)
        {
            return ChartRenderer.VorlaufRuecklaufModell(
                Text_("GEBB_BILD_VORLAUF_RUECKLAUF", "Vorlauf und Rücklauf"),
                ergebnis.VorlaufC, ergebnis.RuecklaufC,
                ergebnis.AuslegungVorlaufC, ergebnis.AuslegungRuecklaufC,
                new ChartRenderer.VorlaufRuecklaufnamen
                {
                    Vorlauf = Text_("GEBB_REIHE_VORLAUF", "Vorlauf"),
                    Ruecklauf = Text_("GEBB_REIHE_RUECKLAUF", "Rücklauf"),
                    AuslegungVorlauf = Text_("GEBB_REIHE_AUSLEGUNG_VORLAUF", "Auslegung Vorlauf"),
                    AuslegungRuecklauf = Text_("GEBB_REIHE_AUSLEGUNG_RUECKLAUF", "Auslegung Rücklauf"),
                });
        }

        /// <summary>
        /// Die Herleitungszeilen des Abschnitts „Kältebedarf" (Stufe KU1, Kühlkonzept 8.3, 8.4):
        /// wie der Kältebedarf dieses Gebäudes entsteht — Bestandsweg (0 mit Hinweis, F-K18),
        /// wirksame Kühlung (Sollwert, Grenze, Kanal), eingeschaltet ohne Sollwert, oder freier
        /// Lauf ohne Kühlbedarf (Projekt rechnet keine Kälte bzw. Gebäude nicht gekühlt, E32) —
        /// und danach die Grenze der Zahl (K5), die an jeder Kältezahl steht. Die Sätze des Laufs
        /// werden wiederverwendet, wo es sie gibt: ein Text, eine Stelle.
        /// </summary>
        internal static List<string> Kaelteherleitung(GebaeudeBedarfErgebnis e)
        {
            var zeilen = new List<string>();
            if (e == null || !e.Erfolgreich) return zeilen;

            CultureInfo k = CultureInfo.CurrentCulture;
            string obere = (e.ObereRaumtemperaturC ?? 0.0).ToString("N1", k);

            if (e.KaelteBestandsweg)
                zeilen.Add(string.Format(k, MyResource.Resource.SIMENG_KAELTE_BESTANDSWEG, e.Name));
            else if (e.KuehlSollwertC is double soll)
                zeilen.Add(string.Format(k, Text_("GEBB_HRL_KAELTE_WIRKSAM",
                                                  "Gekühlt auf {0} °C, Kühlleistungsgrenze {1}. Der Kältebedarf geht in den Kanal Kühlung; Wärmeerzeuger decken keine Kälte."),
                                         soll.ToString("N1", k),
                                         e.KuehlleistungMaxKw is double kw
                                             ? kw.ToString("N1", k) + " kW"
                                             : Text_("GEBB_UNBEGRENZT", "unbegrenzt")));
            else if (!e.KuehlbetriebProjekt)
                zeilen.Add(string.Format(k, Text_("GEBB_HRL_KAELTE_PROJEKT_AUS",
                                                  "Das Projekt rechnet keine Kälte (Projekteinstellung „Kühlung rechnen“ aus): Das Gebäude wird nicht gekühlt und läuft frei — die Raumluft darf über {0} °C steigen, die Überhitzungsstunden zählen die Stunden darüber. Einen Kühlbedarf gibt es nicht."),
                                         obere));
            else if (e.KuehlungAktiv)
                zeilen.Add(string.Format(k, MyResource.Resource.SIMENG_KAELTE_OHNE_SOLLWERT, e.Name));
            else
                zeilen.Add(string.Format(k, Text_("GEBB_HRL_KAELTE_NICHT_GEKUEHLT",
                                                  "Das Gebäude wird nicht gekühlt und läuft frei: Die Raumluft darf über {0} °C steigen, die Überhitzungsstunden zählen die Stunden darüber. Einen Kühlbedarf gibt es nicht."),
                                         obere));

            zeilen.Add(SimulationKaeltebedarf.GrenzeFeuchte);
            return zeilen;
        }

        /// <summary>
        /// Das Bild der Kältelast (Stufe KU1, Kühlkonzept 8.4) — dieselbe Bildform wie die
        /// Wärmelast (normiert auf den Jahreshöchstwert), aber ein EIGENES Bild: im Wärmebild
        /// normierte der Kältewert die Wärmelinie mit (4.2).
        /// </summary>
        private static Zeichenmodell Kaeltemodell(GebaeudeBedarfErgebnis ergebnis, bool sortiert)
        {
            var reihen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(Text_("CHART_ACHSE_KAELTELAST", "Kältelast"),
                                        (double[])ergebnis.KuehlbedarfKwh.Clone(),
                                        Farbrolle.BEDARF)
            };

            return ChartRenderer.GanglinieNormiertModell(
                Text_("CHART_TITEL_KAELTELAST_JAHRESGANGLINIE", "Kältelast Jahresganglinie"),
                reihen,
                Text_("CHART_ACHSE_KAELTELAST", "Kältelast"),
                sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate,
                sortiert);
        }

        /// <summary>
        /// Das Bild „Raumtemperatur" (Stufe G2): Raumluft und operativ mit dem Sollwertband —
        /// gezeichnet im Kern (<c>ChartRenderer.RaumtemperaturModell</c>).
        /// </summary>
        private static Zeichenmodell Raumtemperaturmodell(GebaeudeBedarfErgebnis ergebnis)
            => Raumtemperaturmodell(ergebnis.RaumtemperaturC, ergebnis.OperativeTemperaturC,
                                    ergebnis.HeizsollwertC, ergebnis.ObereRaumtemperaturC);

        /// <summary>Das Bild „Raumtemperatur" EINER Zone (Stufe G6b) — dasselbe Bild mit den Reihen der Zone.</summary>
        private static Zeichenmodell Raumtemperaturmodell(GebaeudeBedarfZone zone)
            => Raumtemperaturmodell(zone.RaumtemperaturC, zone.OperativeTemperaturC,
                                    zone.HeizsollwertC, zone.ObereRaumtemperaturC);

        private static Zeichenmodell Raumtemperaturmodell(double[] raumluft, double[] operativ, double[] heizsollwert,
                                                          double? obere)
        {
            return ChartRenderer.RaumtemperaturModell(
                Text_("GEBB_BILD_RAUMTEMPERATUR", "Raumtemperatur"),
                raumluft, operativ, heizsollwert, obere,
                new ChartRenderer.Raumtemperaturnamen
                {
                    Raumluft = Text_("GEBB_REIHE_RAUMLUFT", "Raumluft"),
                    Operativ = Text_("GEBB_REIHE_OPERATIV", "operative Temperatur"),
                    Heizsollwert = Text_("GEBB_REIHE_HEIZSOLLWERT", "Heizsollwert"),
                    ObereGrenze = Text_("GEBB_REIHE_OBERE_GRENZE", "obere Raumtemperatur"),
                    Achse = "°C"
                });
        }

        /// <summary>
        /// Die Jahresganglinie des Gebäudes — dasselbe Bild wie B1 der Ergebnisseite:
        /// normiert auf den Jahreshöchstwert, x wahlweise Monatsgrenzen oder die vier
        /// Stundenmarken, Farbe <c>F_BEDARF</c>.
        /// </summary>
        /// <param name="sortiert">Dauerlinie statt Ganglinie.</param>
        private static Zeichenmodell Bedarfsmodell(GebaeudeBedarfErgebnis ergebnis, bool sortiert)
            => Lastmodell(ergebnis.Stundenwerte, sortiert);

        /// <summary>Die Jahresganglinie einer Lastreihe in kW — des Gebäudes oder EINER Zone (Stufe G6b).</summary>
        private static Zeichenmodell Lastmodell(double[] werte, bool sortiert)
        {

            var reihen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(Text_("CHART_ACHSE_WAERMELAST", "Wärmelast"),
                                        Array.ConvertAll(werte, x => (double)x),
                                        Farbrolle.BEDARF)
            };

            return ChartRenderer.GanglinieNormiertModell(
                Text_("CHART_TITEL_WAERMELAST_JAHRESGANGLINIE", "Wärmelast Jahresganglinie"),
                reihen,
                Text_("CHART_ACHSE_WAERMELAST", "Wärmelast"),
                sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate,
                sortiert);
        }

        /// <summary>Der Klick auf das Farbfeld eines Legendeneintrags.</summary>
        private static Task FarbeSetzen(Farbrolle rolle, Farbe farbe)
        {
            Diagrammfarben.Setze(rolle, farbe);
            return Task.CompletedTask;
        }

        /// <summary>„Hausfarbe": Der Eintrag fällt aus der Einstellung.</summary>
        private static Task FarbeZuruecksetzen(Farbrolle rolle)
        {
            Diagrammfarben.Zuruecksetzen(rolle);
            return Task.CompletedTask;
        }

        /// <summary>Die zwölf Zeilenbeschriftungen der Monatstabelle (mit Doppelpunkt).</summary>
        private static string[] Monatsnamen()
        {
            var namen = new string[12];
            for (int m = 0; m < 12; m++)
                namen[m] = Text_("ALLG_MONAT_" + (m + 1), MONATE_DE[m]) + ":";
            return namen;
        }

        private static readonly string[] MONATE_DE =
        { "Januar", "Februar", "März", "April", "Mai", "Juni",
          "Juli", "August", "September", "Oktober", "November", "Dezember" };

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
