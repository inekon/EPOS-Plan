using System;
using System.Collections.Generic;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Uebernahme - zwei Trockenlaeufe (Fachkonzept 5.1, Zeilen 8-9) und seit dem
    /// 15.09.2026 die zwei zugehoerigen SCHREIBaktionen (Fachkonzept 5.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Vorschau und Lauf stehen beieinander</b> - und teilen sich Gewerkliste,
    /// Merkmalsaufloesung und Vorbedingung. Das ist der Grund, warum die zwei
    /// Schreibaktionen hier liegen und nicht in <see cref="KiAktionenSchreiben"/>: Der
    /// Trockenlauf IST ihre Vorschau, und zwei Fassungen davon koennten auseinanderlaufen.
    /// </para>
    /// <para>
    /// Die zwei <c>*_vorschau</c>-Aktionen SCHREIBEN NICHTS. Sie bleiben auch fuer sich
    /// genommen nuetzlich: Sie beantworten, ob eine Uebernahme ueberhaupt etwas bewirken
    /// wuerde - ohne dass jemand eine Bestaetigung wegklicken muss.
    /// </para>
    /// </remarks>
    internal static class KiAktionenUebernahme
    {
        // =====================================================================
        // uebernahme_vorschau
        // =====================================================================

        /// <summary>
        /// Trockenlauf der Komponenten-Uebernahme. Andockpunkt
        /// <c>KomponentenUebernahmeCtrl.Planen(int, int, string)</c>; zulaessige Gewerke aus
        /// <c>KomponentenUebernahmeCtrl.Plaene</c>, Pruefung ueber <c>Unterstuetzt(string)</c>.
        /// </summary>
        internal static KiAktion UebernahmeVorschau()
        {
            return new KiAktion(
                name: "uebernahme_vorschau",
                zweck: KiAktionsTexte.ZweckUebernahmeVorschau,
                titel: KiAktionsTexte.TitelUebernahmeVorschau,
                beispiel: KiAktionsTexte.BeispielUebernahmeVorschau,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "KomponentenUebernahmeCtrl.Planen",
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlVonProjekt, name: "von_projekt",
                                             anzeigename: KiAktionsTexte.VonProjektName),
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlNachProjekt, name: "nach_projekt",
                                             anzeigename: KiAktionsTexte.NachProjektName),
                    new KiParameter("gewerk", KiParameterTyp.Aufzaehlung, KiAktionsTexte.ErlGewerk,
                                    anzeigename: KiAktionsTexte.GewerkName, werte: Gewerke())
                },
                vorbedingung: a =>
                {
                    int von = KiHilfe.ProjektId(a, "von_projekt");
                    int nach = KiHilfe.ProjektId(a, "nach_projekt");
                    string gewerk = a.Text("gewerk");

                    if (von == nach) return KiAktionsTexte.GleicheProjekte;

                    string grund = KiHilfe.ProjektMussAufloesbarSein(a, "von_projekt");
                    if (grund != null) return grund;
                    grund = KiHilfe.ProjektMussAufloesbarSein(a, "nach_projekt");
                    if (grund != null) return grund;

                    if (!KomponentenUebernahmeCtrl.Unterstuetzt(gewerk))
                        return string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.GewerkNichtUnterstuetzt,
                                             gewerk, string.Join(", ", Gewerke()));
                    return null;
                },
                ausfuehren: a =>
                {
                    int von = KiHilfe.ProjektId(a, "von_projekt");
                    int nach = KiHilfe.ProjektId(a, "nach_projekt");
                    string gewerk = a.Text("gewerk");

                    KomponentenUebernahmeCtrl.Vorschau v =
                        new KomponentenUebernahmeCtrl().Planen(von, nach, gewerk);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "von_projekt", von,
                        "nach_projekt", nach,
                        "gewerk", gewerk,
                        "moeglich", v.Moeglich,
                        "nichts_zu_tun", v.NichtsZuTun,
                        "anlegen", v.Anlegen.Count,
                        "ersetzen", v.Gleichziehen.Count,
                        "entfernen", v.Entfernen.Count,
                        "grund", KiHilfe.Text(v.Grund)));

                    string text = v.Moeglich
                        ? string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.UebernahmeMoeglich,
                                        v.Anlegen.Count, v.Gleichziehen.Count, v.Entfernen.Count)
                        : string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.UebernahmeNichtMoeglich, v.Grund);

                    KiErgebnis e = KiErgebnis.Ok(text, zeilen, anzahl: 1);
                    if (!string.IsNullOrWhiteSpace(v.Klartext)) e.MitMeldungen(new[] { v.Klartext.Trim() });
                    return e;
                });
        }

        /// <summary>
        /// Die unterstuetzten Gewerke - aus der Landkarte des Controllers, nicht aus einer
        /// zweiten Liste. Was dort fehlt, kann der Assistent nicht anbieten.
        /// </summary>
        private static string[] Gewerke()
        {
            var namen = new List<string>(KomponentenUebernahmeCtrl.Plaene.Keys);
            namen.Sort(StringComparer.Ordinal);
            return namen.ToArray();
        }

        // =====================================================================
        // merkmal_vorschau
        // =====================================================================

        /// <summary>
        /// Trockenlauf der Merkmals-Uebernahme. Andockpunkt
        /// <c>MerkmalUebernahmeCtrl.Pruefe(int, int, AbweichungsErmittler.Merkmal)</c>;
        /// Sperrspalten ueber <c>IstSchluesselspalte(string)</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// BEWUSSTE ABWEICHUNG: <c>merkmal</c> ist ein TEXT und keine Aufzaehlung, obwohl es
        /// eine feste Werteliste gibt. <c>AbweichungsErmittler.Felder</c> fuehrt 54
        /// Merkmale; als <c>enum</c> im Werkzeugkatalog waeren das rund 1,5 KB, die bei
        /// JEDER Modellanfrage mitgingen - mehr als der ganze uebrige Katalog
        /// (Fachkonzept 3.3, Kostenzeile). Der Wert wird stattdessen gegen dieselbe Liste
        /// geprueft, und die Absage nennt alle zulaessigen Schluessel; das Modell kann sich
        /// also in EINER Korrekturrunde fangen. Geraten wird nichts.
        /// </para>
        /// </remarks>
        internal static KiAktion MerkmalVorschau()
        {
            return new KiAktion(
                name: "merkmal_vorschau",
                zweck: KiAktionsTexte.ZweckMerkmalVorschau,
                titel: KiAktionsTexte.TitelMerkmalVorschau,
                beispiel: KiAktionsTexte.BeispielMerkmalVorschau,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "MerkmalUebernahmeCtrl.Pruefe",
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlVonProjekt, name: "von_projekt",
                                             anzeigename: KiAktionsTexte.VonProjektName),
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlNachProjekt, name: "nach_projekt",
                                             anzeigename: KiAktionsTexte.NachProjektName),
                    new KiParameter("merkmal", KiParameterTyp.Text, KiAktionsTexte.ErlMerkmal,
                                    anzeigename: KiAktionsTexte.MerkmalName, maxLaenge: 120)
                },
                vorbedingung: a =>
                {
                    int von = KiHilfe.ProjektId(a, "von_projekt");
                    int nach = KiHilfe.ProjektId(a, "nach_projekt");

                    if (von == nach) return KiAktionsTexte.GleicheProjekte;

                    string grund = KiHilfe.ProjektMussAufloesbarSein(a, "von_projekt");
                    if (grund != null) return grund;
                    grund = KiHilfe.ProjektMussAufloesbarSein(a, "nach_projekt");
                    if (grund != null) return grund;

                    if (Merkmal(a.Text("merkmal")) == null)
                        return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.MerkmalUnbekannt,
                                             a.Text("merkmal"), string.Join(", ", Merkmalsschluessel()));
                    return null;
                },
                ausfuehren: a =>
                {
                    int von = KiHilfe.ProjektId(a, "von_projekt");
                    int nach = KiHilfe.ProjektId(a, "nach_projekt");
                    AbweichungsErmittler.Merkmal f = Merkmal(a.Text("merkmal"));

                    MerkmalUebernahmeCtrl.Befund b = MerkmalUebernahmeCtrl.Pruefe(von, nach, f);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "von_projekt", von,
                        "nach_projekt", nach,
                        "merkmal", Schluessel(f),
                        "gewerk", KiHilfe.Text(f.Gewerk),
                        "label", KiHilfe.Text(f.Label),
                        "einheit", KiHilfe.Text(f.Einheit),
                        "schluesselspalte", MerkmalUebernahmeCtrl.IstSchluesselspalte(f.Spalte),
                        "moeglich", b.Moeglich,
                        "gleichstand", b.Gleichstand,
                        "wert_quelle", KiHilfe.Text(b.Quelle.Anzeigewert),
                        "wert_ziel", KiHilfe.Text(b.Ziel.Anzeigewert),
                        "zeilen_quelle", b.Quelle.Anzahl,
                        "zeilen_ziel", b.Ziel.Anzahl,
                        "grund", KiHilfe.Text(b.Grund)));

                    string text;
                    if (!b.Moeglich)
                        text = string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.MerkmalNichtMoeglich, b.Grund);
                    else if (b.Gleichstand)
                        text = string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.MerkmalGleichstand,
                                             f.Label, b.Ziel.Anzeigewert);
                    else
                        text = string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.MerkmalMoeglich,
                                             f.Label, b.Ziel.Anzeigewert, b.Quelle.Anzeigewert);

                    return KiErgebnis.Ok(text, zeilen, anzahl: 1);
                });
        }

        /// <summary>Schluessel eines Merkmals: <c>Tabelle.Spalte</c>.</summary>
        internal static string Schluessel(AbweichungsErmittler.Merkmal f)
        {
            return f == null ? "" : f.Tabelle + "." + f.Spalte;
        }

        /// <summary>Sucht ein Merkmal ueber seinen Schluessel; <c>null</c>, wenn unbekannt.</summary>
        internal static AbweichungsErmittler.Merkmal Merkmal(string schluessel)
        {
            if (string.IsNullOrWhiteSpace(schluessel)) return null;
            string gesucht = schluessel.Trim();

            foreach (AbweichungsErmittler.Merkmal f in AbweichungsErmittler.Felder)
                if (string.Equals(Schluessel(f), gesucht, StringComparison.OrdinalIgnoreCase)) return f;
            return null;
        }

        /// <summary>Alle Merkmalsschluessel - Grundlage der Absage bei unbekanntem Wert.</summary>
        internal static IReadOnlyList<string> Merkmalsschluessel()
        {
            var namen = new List<string>();
            foreach (AbweichungsErmittler.Merkmal f in AbweichungsErmittler.Felder)
                namen.Add(Schluessel(f));
            return namen;
        }

        // =====================================================================
        // komponente_uebernehmen  (Fachkonzept 5.2, nachgezogen am 15.09.2026)
        // =====================================================================

        /// <summary>
        /// Uebernimmt ein ganzes Gewerk von einem Projekt in ein anderes. Andockpunkt
        /// <c>KomponentenUebernahmeCtrl.Uebernehmen(int, int, string, out string, out string)</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Der Trockenlauf ist Pflicht und laeuft ZWEIMAL</b> - einmal als
        /// <see cref="KiAktion.Vorbedingung"/>, einmal als <see cref="KiAktion.Vorschau"/>.
        /// Das ist kein Versehen: Die Vorbedingung entscheidet, ob ueberhaupt gefragt
        /// wird, die Vorschau baut den Satz, den der Anwender bestaetigt. Beide lesen nur
        /// (<c>Planen</c> schreibt nichts), und beide muessen denselben Stand sehen wie
        /// der Lauf danach - deshalb wird nicht zwischengespeichert.
        /// </para>
        /// <para>
        /// <b>NICHT umkehrbar.</b> Die Uebernahme legt an, zieht gleich UND entfernt; der
        /// Vorzustand des Zielgewerks liesse sich nur als ganzes Projekt zurueckholen, und
        /// Loeschen steht nicht im Register (Fachkonzept 5.4). Die Bestaetigung sagt das
        /// so - und davor steht der Sicherungspunkt (Fachkonzept 4.4, Punkt 1).
        /// </para>
        /// <para>
        /// <b>Die Transaktionsklammer bringt der Bestand mit</b>
        /// (<c>KomponentenUebernahmeCtrl:283</c>): Ein Teilzustand aus halb uebernommenen
        /// Komponenten ist damit ausgeschlossen, ohne dass diese Aktion etwas dazutut.
        /// </para>
        /// </remarks>
        internal static KiAktion KomponenteUebernehmen()
        {
            return new KiAktion(
                name: "komponente_uebernehmen",
                zweck: KiAktionsTexte.ZweckKomponenteUebernehmen,
                titel: KiAktionsTexte.TitelKomponenteUebernehmen,
                beispiel: KiAktionsTexte.BeispielKomponenteUebernehmen,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "KomponentenUebernahmeCtrl.Uebernehmen",
                wirkung: KiAktionsTexte.WirkungKomponenteUebernehmen,
                umkehrbar: false,
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlVonProjekt, name: "von_projekt",
                                             anzeigename: KiAktionsTexte.VonProjektName),
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlNachProjekt, name: "nach_projekt",
                                             anzeigename: KiAktionsTexte.NachProjektName),
                    new KiParameter("gewerk", KiParameterTyp.Aufzaehlung, KiAktionsTexte.ErlGewerk,
                                    anzeigename: KiAktionsTexte.GewerkName, werte: Gewerke())
                },
                vorbedingung: a =>
                {
                    string grund = UebernahmeGrund(a);
                    if (grund != null) return grund;

                    KomponentenUebernahmeCtrl.Vorschau v = Planen(a);
                    if (!v.Moeglich)
                        return string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.UebernahmeNichtMoeglich, v.Grund);
                    if (v.NichtsZuTun)
                        return KiAktionsTexte.UebernahmeNichtsZuTun;

                    return KiSchreibschutz.Gesperrt("Tab_Projekt", "ID",
                                                    KiHilfe.ProjektId(a, "nach_projekt"));
                },
                vorschau: a =>
                {
                    KomponentenUebernahmeCtrl.Vorschau v = Planen(a);

                    string satz = string.Format(CultureInfo.CurrentCulture,
                        KiAktionsTexte.VorschauKomponenteUebernehmen,
                        a.Text("gewerk"),
                        KiHilfe.ProjektName(KiHilfe.ProjektId(a, "von_projekt")),
                        KiHilfe.ProjektName(KiHilfe.ProjektId(a, "nach_projekt")),
                        v.Anlegen.Count, v.Gleichziehen.Count, v.Entfernen.Count);

                    // Der KLARTEXT des Bestands zaehlt die betroffenen Komponenten
                    // namentlich auf - er ist der eigentliche Gegenstand der
                    // Bestaetigung und darf deshalb nicht weggekuerzt werden.
                    return string.IsNullOrWhiteSpace(v.Klartext) ? satz : satz + "\n" + v.Klartext.Trim();
                },
                ausfuehren: a =>
                {
                    int von = KiHilfe.ProjektId(a, "von_projekt");
                    int nach = KiHilfe.ProjektId(a, "nach_projekt");
                    string gewerk = a.Text("gewerk");

                    string fehler, hinweise;
                    bool ok = new KomponentenUebernahmeCtrl()
                        .Uebernehmen(von, nach, gewerk, out fehler, out hinweise);

                    if (!ok)
                        return KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture,
                                          KiAktionsTexte.UebernahmeFehlgeschlagen, gewerk,
                                          KiHilfe.Text(fehler)));

                    string datumsmeldung = KiAktionenSchreiben.AenderungsdatumSetzen(nach);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "von_projekt", von,
                        "nach_projekt", nach,
                        "gewerk", gewerk,
                        "hinweise", KiHilfe.Text(hinweise)));

                    KiErgebnis e = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.UebernahmeAusgefuehrt,
                                      gewerk, KiHilfe.ProjektName(nach)),
                        zeilen, anzahl: 1);

                    var meldungen = new List<string>();
                    if (!string.IsNullOrWhiteSpace(hinweise)) meldungen.Add(hinweise.Trim());
                    if (datumsmeldung != null) meldungen.Add(datumsmeldung);
                    return e.MitMeldungen(meldungen);
                });
        }

        /// <summary>Die gemeinsame Vorbedingung beider Uebernahmewege.</summary>
        private static string UebernahmeGrund(KiAufruf a)
        {
            if (KiHilfe.ProjektId(a, "von_projekt") == KiHilfe.ProjektId(a, "nach_projekt"))
                return KiAktionsTexte.GleicheProjekte;

            string grund = KiHilfe.ProjektMussAufloesbarSein(a, "von_projekt");
            if (grund != null) return grund;
            grund = KiHilfe.ProjektMussAufloesbarSein(a, "nach_projekt");
            if (grund != null) return grund;

            if (!KomponentenUebernahmeCtrl.Unterstuetzt(a.Text("gewerk")))
                return string.Format(CultureInfo.CurrentCulture,
                                     KiAktionsTexte.GewerkNichtUnterstuetzt,
                                     a.Text("gewerk"), string.Join(", ", Gewerke()));
            return null;
        }

        /// <summary>Der Trockenlauf zu diesem Aufruf - er schreibt nichts.</summary>
        private static KomponentenUebernahmeCtrl.Vorschau Planen(KiAufruf a)
        {
            return new KomponentenUebernahmeCtrl().Planen(
                KiHilfe.ProjektId(a, "von_projekt"),
                KiHilfe.ProjektId(a, "nach_projekt"),
                a.Text("gewerk"));
        }

        // =====================================================================
        // merkmal_uebernehmen  (Fachkonzept 5.2, nachgezogen am 15.09.2026)
        // =====================================================================

        /// <summary>
        /// Schreibt EIN Merkmal aus einem Projekt in ein anderes. Andockpunkt
        /// <c>MerkmalUebernahmeCtrl.Schreibe(Befund, int, Merkmal, out string)</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>UMKEHRBAR</b>, und das ist hier woertlich zu nehmen: Der Vorzustand ist EIN
        /// Wert, er steht im Befund (<c>b.Ziel.Anzeigewert</c>), er steht in der
        /// Bestaetigung und er steht im Ergebnis. Wer ihn zurueckhaben will, ruft dieselbe
        /// Aktion in der Gegenrichtung - mit eigener Bestaetigung.
        /// </para>
        /// <para>
        /// <b>Der Befund geht IN die Schreibmethode hinein</b> (anders als bei der
        /// Komponentenuebernahme): <c>Schreibe</c> nimmt ihn entgegen und schreibt genau
        /// den Wert, der darin steht. Ermittelt wird er unmittelbar davor - ein
        /// zwischengespeicherter Befund koennte einen Stand beschreiben, den es nicht mehr
        /// gibt.
        /// </para>
        /// </remarks>
        internal static KiAktion MerkmalUebernehmen()
        {
            return new KiAktion(
                name: "merkmal_uebernehmen",
                zweck: KiAktionsTexte.ZweckMerkmalUebernehmen,
                titel: KiAktionsTexte.TitelMerkmalUebernehmen,
                beispiel: KiAktionsTexte.BeispielMerkmalUebernehmen,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "MerkmalUebernahmeCtrl.Schreibe",
                wirkung: KiAktionsTexte.WirkungMerkmalUebernehmen,
                umkehrbar: true,
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlVonProjekt, name: "von_projekt",
                                             anzeigename: KiAktionsTexte.VonProjektName),
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlNachProjekt, name: "nach_projekt",
                                             anzeigename: KiAktionsTexte.NachProjektName),
                    new KiParameter("merkmal", KiParameterTyp.Text, KiAktionsTexte.ErlMerkmal,
                                    anzeigename: KiAktionsTexte.MerkmalName, maxLaenge: 120)
                },
                vorbedingung: a =>
                {
                    if (KiHilfe.ProjektId(a, "von_projekt") == KiHilfe.ProjektId(a, "nach_projekt"))
                        return KiAktionsTexte.GleicheProjekte;

                    string grund = KiHilfe.ProjektMussAufloesbarSein(a, "von_projekt");
                    if (grund != null) return grund;
                    grund = KiHilfe.ProjektMussAufloesbarSein(a, "nach_projekt");
                    if (grund != null) return grund;

                    AbweichungsErmittler.Merkmal f = Merkmal(a.Text("merkmal"));
                    if (f == null)
                        return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.MerkmalUnbekannt,
                                             a.Text("merkmal"), string.Join(", ", Merkmalsschluessel()));

                    if (MerkmalUebernahmeCtrl.IstSchluesselspalte(f.Spalte))
                        return string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.MerkmalGesperrt, Schluessel(f));

                    MerkmalUebernahmeCtrl.Befund b = Befinden(a, f);
                    if (!b.Moeglich)
                        return string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.MerkmalNichtMoeglich, b.Grund);
                    if (b.Gleichstand)
                        return string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.MerkmalGleichstand, f.Label,
                                             b.Ziel.Anzeigewert);

                    return KiSchreibschutz.Gesperrt("Tab_Projekt", "ID",
                                                    KiHilfe.ProjektId(a, "nach_projekt"));
                },
                vorschau: a =>
                {
                    AbweichungsErmittler.Merkmal f = Merkmal(a.Text("merkmal"));
                    if (f == null) return KiAktionsTexte.MerkmalOhneVorschau;

                    MerkmalUebernahmeCtrl.Befund b = Befinden(a, f);

                    return string.Format(CultureInfo.CurrentCulture,
                        KiAktionsTexte.VorschauMerkmalUebernehmen,
                        f.Label,
                        KiHilfe.ProjektName(KiHilfe.ProjektId(a, "nach_projekt")),
                        b.Ziel.Anzeigewert, b.Quelle.Anzeigewert,
                        KiHilfe.Text(f.Einheit));
                },
                ausfuehren: a =>
                {
                    int nach = KiHilfe.ProjektId(a, "nach_projekt");
                    AbweichungsErmittler.Merkmal f = Merkmal(a.Text("merkmal"));
                    MerkmalUebernahmeCtrl.Befund b = Befinden(a, f);

                    // Der Vorzustand wird VOR dem Schreiben festgehalten - danach steht
                    // er nirgends mehr, und ohne ihn waere „umkehrbar" eine Behauptung.
                    string vorher = b.Ziel.Anzeigewert;

                    string fehler;
                    if (!MerkmalUebernahmeCtrl.Schreibe(b, nach, f, out fehler))
                        return KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture,
                                          KiAktionsTexte.MerkmalFehlgeschlagen, f.Label,
                                          KiHilfe.Text(fehler)));

                    string datumsmeldung = KiAktionenSchreiben.AenderungsdatumSetzen(nach);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "von_projekt", KiHilfe.ProjektId(a, "von_projekt"),
                        "nach_projekt", nach,
                        "merkmal", Schluessel(f),
                        "label", KiHilfe.Text(f.Label),
                        "einheit", KiHilfe.Text(f.Einheit),
                        "wert_vorher", KiHilfe.Text(vorher),
                        "wert_nachher", KiHilfe.Text(b.Quelle.Anzeigewert)));

                    KiErgebnis e = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.MerkmalGeschrieben,
                                      f.Label, vorher, b.Quelle.Anzeigewert),
                        zeilen, anzahl: 1);

                    if (datumsmeldung != null) e.MitMeldungen(new[] { datumsmeldung });
                    return e;
                });
        }

        /// <summary>Der Befund zu diesem Aufruf - er schreibt nichts.</summary>
        private static MerkmalUebernahmeCtrl.Befund Befinden(KiAufruf a, AbweichungsErmittler.Merkmal f)
        {
            return MerkmalUebernahmeCtrl.Pruefe(
                KiHilfe.ProjektId(a, "von_projekt"),
                KiHilfe.ProjektId(a, "nach_projekt"), f);
        }
    }
}
