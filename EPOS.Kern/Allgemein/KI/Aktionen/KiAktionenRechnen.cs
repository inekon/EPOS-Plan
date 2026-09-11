using System;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die RECHENAKTIONEN der Stufe 3 (Fachkonzept 5.3, Auftrag #201 Punkt 5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Drei Aktionen, zwei Bauarten.</b> <c>simulation_rechnen</c> rechnet im KERN —
    /// <see cref="SimulationRunner"/> ist plattformfrei und braucht keine offene Maske.
    /// <c>peak_ziel_bestimmen</c> und <c>flotte_bewerten</c> rechnen dagegen IN DER
    /// OFFENEN ANSICHT: Sie rufen den Weg, den der Anwender sonst mit dem Knopf
    /// ausloest, und ihr Ergebnis steht danach dort, wo er es erwartet — in der Ansicht
    /// und nicht nur im Chat (Auftrag #201: „setzen das Ergebnis in die OFFENE Ansicht").
    /// </para>
    /// <para>
    /// <b>Warum die zwei nicht im Kern rechnen.</b> Der Rechenweg der
    /// Stromspeicher-Ansicht haengt an Staenden, die nur sie fuehrt: dem importierten
    /// Zeitreihensatz, dem Arbeitsstand der Flotte, dem gerechneten Simulationslauf, aus
    /// dem die EPOS-Reihen stammen. Ihn im Kern nachzubauen hiesse, diese Staende ein
    /// zweites Mal zu beschaffen — und dann rechnete der Assistent etwas anderes als der
    /// Knopf daneben. Die Ansicht meldet ihre Wege deshalb als
    /// <see cref="KiMaskenhaken.Rechenweg"/> an; der Schluessel ist der AKTIONSNAME, es
    /// gibt keinen zweiten Namensraum.
    /// </para>
    /// <para>
    /// <b>Fortschritt und Abbruch.</b> Alle drei laufen ueber
    /// <see cref="KiAktion.AusfuehrenLang"/> und bekommen damit die
    /// <see cref="KiLaufumgebung"/>: Sie melden ihre Phasen und pruefen zwischen ihnen
    /// die Abbruchmarke. Ein Abbruch hinterlaesst keinen halben Zustand — bei
    /// <c>simulation_rechnen</c>, weil vor dem Speichern abgebrochen wird, bei den zwei
    /// anderen, weil die Ansicht ihr Ergebnis erst nach dem vollstaendigen Lauf setzt.
    /// </para>
    /// <para>
    /// <b>Die Einlaeufigkeit kommt vom Ausfuehrer</b> (Fachkonzept 3.4, Pflicht 1) und
    /// nicht von hier: Zwei gleichzeitige Anforderungen weist
    /// <see cref="KiAusfuehrung"/> mit „es laeuft bereits etwas" ab, und zwar fuer jede
    /// Aktion gleich. Eine zweite Sperre an dieser Stelle waere die zweite Wahrheit.
    /// </para>
    /// </remarks>
    internal static class KiAktionenRechnen
    {
        // =====================================================================
        // simulation_rechnen
        // =====================================================================

        /// <summary>
        /// Rechnet die Simulation eines Projekts — auf Wunsch mit Speichern.
        /// Andockpunkt <c>SimulationRunner.Simuliere</c> /
        /// <c>SimuliereUndSpeichere</c>.
        /// </summary>
        /// <remarks>
        /// <b>Speichern ERSETZT den Vorgaengerlauf</b> (Fachkonzept 4.4, Punkt 4) — das
        /// steht so in der Wirkung und damit woertlich in der Bestaetigung. Ohne
        /// <c>speichern</c> wird gerechnet und nichts geschrieben; die Aktion bleibt
        /// trotzdem bestaetigungspflichtig, weil ein Lauf Minuten dauert und die
        /// prozessweiten Staende des Rechenkerns belegt.
        /// </remarks>
        internal static KiAktion SimulationRechnen()
        {
            return new KiAktion(
                name: "simulation_rechnen",
                zweck: KiAktionsTexte.ZweckSimulationRechnen,
                titel: KiAktionsTexte.TitelSimulationRechnen,
                beispiel: KiAktionsTexte.BeispielSimulationRechnen,
                stufe: Schutzstufe.Rechnen,
                andockpunkt: "SimulationRunner.SimuliereUndSpeichere",
                umkehrbar: false,
                wirkung: KiAktionsTexte.WirkungSimulationRechnen,
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(),
                    new KiParameter("speichern", KiParameterTyp.Wahrheitswert,
                                    KiAktionsTexte.ErlSimulationSpeichern,
                                    pflicht: false,
                                    anzeigename: KiAktionsTexte.SimulationSpeichernName)
                },
                vorbedingung: a =>
                {
                    string grund = KiHilfe.ProjektMussAufloesbarSein(a);
                    if (grund != null) return grund;

                    if (a.Wahrheit("speichern"))
                    {
                        string lesemodus = SimulationLaufCtrl.LesemodusGrund();
                        if (lesemodus != null) return lesemodus;
                    }

                    return null;
                },
                vorschau: a =>
                {
                    KiHilfe.Auswahl wahl = KiHilfe.ProjektWaehlen(a);
                    return string.Format(CultureInfo.CurrentCulture,
                                         a.Wahrheit("speichern")
                                             ? MyResource.Resource.KI_AKTION_SIM_VORSCHAU_SPEICHERN
                                             : MyResource.Resource.KI_AKTION_SIM_VORSCHAU,
                                         wahl.Name);
                },
                ausfuehrenLang: (a, u) =>
                {
                    KiHilfe.Auswahl wahl = KiHilfe.ProjektWaehlen(a);
                    if (!wahl.Ok) return KiErgebnis.Abgelehnt(wahl.Fehler);

                    bool speichern = a.Wahrheit("speichern");

                    u.Melde(null, string.Format(CultureInfo.CurrentCulture,
                                                MyResource.Resource.KI_AKTION_SIM_LAEUFT, wahl.Name));
                    u.AbbruchPruefen();

                    var runner = new SimulationRunner();
                    string fehler;
                    int idErgebnis = 0;
                    bool ok;

                    if (speichern)
                    {
                        idErgebnis = runner.SimuliereUndSpeichere(wahl.Id, out fehler);
                        ok = idErgebnis > 0;
                    }
                    else
                    {
                        ok = runner.Simuliere(wahl.Id, out fehler);
                    }

                    // Abgebrochen wird NACH dem Lauf ausgewertet: Der Runner kennt keine
                    // Abbruchmarke, und ein halb gespeicherter Ergebnisstand entstuende
                    // erst beim Schreiben - das ist der Punkt, an dem hier geprueft wird.
                    if (u.Abbruch.IsCancellationRequested)
                        return KiErgebnis.Abgebrochen(MyResource.Resource.KI_AUS_ABGEBROCHEN);

                    if (!ok)
                        return KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture,
                                          MyResource.Resource.KI_AKTION_SIM_FEHLER,
                                          wahl.Name, fehler ?? ""));

                    u.Melde(1.0, MyResource.Resource.KI_AKTION_SIM_FERTIG);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "projekt_id", wahl.Id,
                        "gespeichert", speichern,
                        "ergebnis_id", idErgebnis,
                        "lauf_ok", runner.LaufOk));

                    KiErgebnis ergebnis = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture,
                                      speichern
                                          ? MyResource.Resource.KI_AKTION_SIM_OK_GESPEICHERT
                                          : MyResource.Resource.KI_AKTION_SIM_OK,
                                      wahl.Name),
                        zeilen, anzahl: 1);

                    string hinweise = runner.Protokoll == null ? "" : runner.Protokoll.AlsText(true);
                    if (!string.IsNullOrEmpty(hinweise))
                        ergebnis = ergebnis.MitMeldungen(new[] { hinweise });

                    return ergebnis;
                });
        }

        // =====================================================================
        // peak_ziel_bestimmen
        // =====================================================================

        /// <summary>
        /// Bestimmt das wirtschaftliche Peak-Ziel der offenen Stromspeicher-Ansicht
        /// (Bisektion aus Paket P1). Andockpunkt <c>KiMaskenhaken.Rechenweg</c>.
        /// </summary>
        internal static KiAktion PeakZielBestimmen()
        {
            return Ansichtsrechnung(
                "peak_ziel_bestimmen",
                KiAktionsTexte.ZweckPeakZiel,
                KiAktionsTexte.TitelPeakZiel,
                KiAktionsTexte.BeispielPeakZiel,
                KiAktionsTexte.WirkungPeakZiel,
                "FlottenPeakZiel.PeakZielBestimmen",
                MyResource.Resource.KI_AKTION_PEAKZIEL_VORSCHAU,
                MyResource.Resource.KI_AKTION_PEAKZIEL_LAEUFT);
        }

        // =====================================================================
        // flotte_bewerten
        // =====================================================================

        /// <summary>
        /// Rechnet die Flotte der offenen Stromspeicher-Ansicht durch — derselbe Weg wie
        /// ihr Knopf „Berechnen". Andockpunkt <c>KiMaskenhaken.Rechenweg</c>.
        /// </summary>
        internal static KiAktion FlotteBewerten()
        {
            return Ansichtsrechnung(
                "flotte_bewerten",
                KiAktionsTexte.ZweckFlotteBewerten,
                KiAktionsTexte.TitelFlotteBewerten,
                KiAktionsTexte.BeispielFlotteBewerten,
                KiAktionsTexte.WirkungFlotteBewerten,
                "SpeicherFlottenProjektCtrl.Bewerten",
                MyResource.Resource.KI_AKTION_FLOTTE_VORSCHAU,
                MyResource.Resource.KI_AKTION_FLOTTE_LAEUFT);
        }

        // =====================================================================
        // Der gemeinsame Bauplan der zwei Ansichtsrechnungen
        // =====================================================================

        /// <summary>
        /// Baut eine Rechenaktion, die den Weg der OFFENEN Ansicht ruft.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Ein Bauplan fuer beide.</b> Sie unterscheiden sich in Namen, Texten und im
        /// Haken, den sie suchen — nicht im Ablauf: Maske angemeldet? Weg angemeldet?
        /// Lesemodus? Dann rechnen, Fortschritt melden, Ergebnis zurueckgeben. Zwei
        /// Abschriften desselben Ablaufs liefen auseinander.
        /// </para>
        /// <para>
        /// <b>Sie sind KEINE Formularaktionen</b>, obwohl sie die offene Maske brauchen:
        /// Die Modalitaetsweiche des Ausfuehrers gilt der Feldsteuerung (Fachkonzept
        /// 11.4). Eine Rechnung soll ausdruecklich NICHT an einem offenen modalen Dialog
        /// vorbei starten — sie belegt den Rechenkern fuer Minuten.
        /// </para>
        /// <para>
        /// <b>Sie sind datenbankwirksam</b> und bekommen damit den Sicherungspunkt: Der
        /// Weg der Ansicht kann seinen Stand schreiben (die Flottenbewertung legt ihren
        /// Arbeitsstand ab), und im Zweifel zeigt die Vorgabe in die unschaedliche
        /// Richtung (<see cref="KiAktion.Datenbankwirksam"/>).
        /// </para>
        /// </remarks>
        private static KiAktion Ansichtsrechnung(string name, string zweck, string titel,
                                                 string beispiel, string wirkung, string andockpunkt,
                                                 string vorschautext, string lauftext)
        {
            return new KiAktion(
                name: name,
                zweck: zweck,
                titel: titel,
                beispiel: beispiel,
                stufe: Schutzstufe.Rechnen,
                andockpunkt: andockpunkt,
                umkehrbar: false,
                wirkung: wirkung,
                parameter: Array.Empty<KiParameter>(),
                vorbedingung: a => Rechenweggrund(name),
                vorschau: a =>
                {
                    KiDialog eintrag = KiMaskenbruecke.Katalogeintrag(
                        KiMaskennamen.STROMSPEICHER_AUSLEGUNG);
                    return string.Format(CultureInfo.CurrentCulture, vorschautext,
                                         eintrag == null
                                             ? KiMaskennamen.STROMSPEICHER_AUSLEGUNG
                                             : eintrag.Anzeigename);
                },
                ausfuehrenLang: (a, u) =>
                {
                    string grund = Rechenweggrund(name);
                    if (grund != null) return KiErgebnis.Abgelehnt(grund);

                    KiRechenweg weg = KiMaskenbruecke
                        .Haken(KiMaskennamen.STROMSPEICHER_AUSLEGUNG).FindeRechenweg(name);

                    u.Melde(null, lauftext);
                    u.AbbruchPruefen();

                    try
                    {
                        KiErgebnis ergebnis = weg(u).GetAwaiter().GetResult();

                        if (u.Abbruch.IsCancellationRequested)
                            return KiErgebnis.Abgebrochen(MyResource.Resource.KI_AUS_ABGEBROCHEN);

                        return ergebnis ?? KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture,
                                          MyResource.Resource.KI_AUS_KEIN_ERGEBNIS, name));
                    }
                    catch (OperationCanceledException)
                    {
                        return KiErgebnis.Abgebrochen(MyResource.Resource.KI_AUS_ABGEBROCHEN);
                    }
                });
        }

        /// <summary>
        /// Warum diese Rechnung gerade nicht geht; <c>null</c> = sie geht.
        /// </summary>
        /// <remarks>
        /// Drei Fragen in dieser Reihenfolge: Steht die Ansicht ueberhaupt? Bietet sie
        /// diesen Weg an? Erlaubt die Lizenz das Schreiben? Die dritte steht hier, obwohl
        /// der Ausfuehrer sie ohnehin stellt — sie nennt die Ansicht beim Namen, und die
        /// naeher am Anwender kommt zuerst.
        /// </remarks>
        private static string Rechenweggrund(string aktionsname)
        {
            if (!KiMaskenbruecke.IstAngemeldet(KiMaskennamen.STROMSPEICHER_AUSLEGUNG))
                return string.Format(CultureInfo.CurrentCulture,
                                     MyResource.Resource.KI_AKTION_ANSICHT_NICHT_OFFEN,
                                     KiDialogTexte.MaskeSpeicherauslegung);

            if (KiMaskenbruecke.Haken(KiMaskennamen.STROMSPEICHER_AUSLEGUNG)
                               .FindeRechenweg(aktionsname) == null)
                return string.Format(CultureInfo.CurrentCulture,
                                     MyResource.Resource.KI_AKTION_KEIN_RECHENWEG,
                                     aktionsname, KiDialogTexte.MaskeSpeicherauslegung);

            return SimulationLaufCtrl.LesemodusGrund();
        }
    }
}
