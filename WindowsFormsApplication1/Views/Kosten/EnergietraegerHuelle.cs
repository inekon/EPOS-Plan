using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Energieträgerverwaltung (iU9-W4.4) — Nachfolge der
    /// gelöschten Masken <c>Views/Kosten/Form_Energietraeger</c> (535 Z.) und
    /// <c>Views/Kosten/ucFuelSettings</c> (2 103 Z.).
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Die neun SQL-Anweisungen der
    /// Trägerkarte stehen seit dieser Welle im Kern-Controller
    /// <see cref="EnergietraegerPreisCtrl"/>; die Katalogpflege lief schon
    /// vorher über <see cref="EnergietraegerKatalogCtrl"/>. Diese Hülle lädt,
    /// rechnet und schreibt — die Komponenten
    /// <see cref="EnergietraegerDialog"/> und
    /// <see cref="EnergietraegerEinstellungen"/> zeigen nur an.</para>
    ///
    /// <para><b>Die Rechenwege bleiben, wo sie hingehören.</b> Der
    /// Einheitenprüfer (<see cref="EnergieEinheitenPruefung"/>) beantwortet
    /// weiter die Frage, ob eine Regel abgeschaltet werden darf und ob der
    /// Träger kWh erreicht; die Aufschlagssätze rechnet
    /// <see cref="StromAufschlagCtrl"/> bzw.
    /// <see cref="BrennstoffBestandteilCtrl"/>. Es gibt keine zweite Fassung
    /// einer Fachregel, nur einen zweiten Leser.</para>
    ///
    /// <para><b>Ein Fenster, eine WebView.</b> Kostenprofil, Spotpreis-Import,
    /// saisonale Sätze und der Emissionskatalog sind seit Welle 3
    /// Razor-Komponenten; sie erscheinen jetzt in einer <c>Ueberlagerung</c>
    /// desselben Fensters statt in einer zweiten <c>BlazorDialogForm</c>
    /// (Risiko R2).</para>
    /// </summary>
    internal sealed class EnergietraegerHuelle
    {
        /// <summary>Innenmaß des Fensters. Die WinForms-Fassung maß 1084 × 680;
        /// die Trägerkarte steht jetzt untereinander statt in zwei Reitern.</summary>
        private static readonly System.Drawing.Size FENSTER = new System.Drawing.Size(1140, 840);

        private readonly int _projektId;
        private bool Katalogkontext { get { return _projektId <= 0; } }

        private List<EnergyCarrier> _traeger = new List<EnergyCarrier>();
        private EnergyCarrier _gewaehlt;

        // ---- Stand der offenen Trägerkarte ---------------------------------

        private EnergietraegerStand _stand;
        private List<EnergyConversion> _umrechnungen = new List<EnergyConversion>();
        private List<UmrechnungsRegel> _regeln = new List<UmrechnungsRegel>();
        private StromAufschlagModel _aufschlagModell;
        private BrennstoffBestandteilModel _bestandteilModell;

        /// <summary>Live-Werte, immer auf die Basiseinheit normiert (wie im Vorläufer).</summary>
        private double _baseHi, _baseHs, _baseWork, _basePower, _baseGround;

        /// <summary>Der unberührte DB-Zustand für den Historienvergleich.</summary>
        private double _dbHi, _dbHs, _dbWork, _dbPower, _dbGround, _dbCO2, _dbSO2, _dbNOx;

        private EmissionenCtrl _emissionen;
        private int _katalogJahr;
        private string _unternehmensart = DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE;
        private double _co2PreisProjekt;
        private int _idBrennstoff;
        private string _abrechnungseinheit = "";

        private EnergietraegerHuelle(int projektId)
        {
            _projektId = projektId > 0 ? projektId : 0;
        }

        // =====================================================================
        // Einstieg
        // =====================================================================

        /// <summary>
        /// Zeigt die Energieträgerverwaltung.
        /// </summary>
        /// <param name="besitzer">Besitzerfenster (für die mittige Lage).</param>
        /// <param name="projektId">0 = Katalogkontext (Stammdaten).</param>
        /// <param name="traegerId">Vorwahl (KD6 § 9: „Energiekosten…" springt
        /// direkt auf den Träger der Komponente); 0 = der erste.</param>
        internal static void Oeffnen(IWin32Window besitzer, int projektId, int traegerId = 0)
        {
            new EnergietraegerHuelle(projektId).Zeigen(besitzer, traegerId);
        }

        private void Zeigen(IWin32Window besitzer, int traegerId)
        {
            ListeLaden();
            _katalogJahr = KatalogjahrErmitteln(_projektId, out _unternehmensart, out _co2PreisProjekt);

            BlazorDialogForm<EnergietraegerDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["Liste"] = Listeneintraege(),
                ["TraegerVorwahl"] = traegerId > 0 ? (int?)traegerId : null,
                ["Katalogkontext"] = Katalogkontext,
                ["Gruppen"] = Gruppen(),

                ["TraegerLaden"] = new Func<int, EnergietraegerAnsicht>(TraegerLaden),
                ["Nachrechnen"] = new Func<EnergietraegerAnsicht>(Ansicht),
                ["PreisbasisGewechselt"] = EventCallback.Factory.Create<int>(new object(), PreisbasisWechseln),
                ["LeistungsModusGewechselt"] = EventCallback.Factory.Create<bool>(new object(),
                    monat => EnergietraegerPreisCtrl.LeistungsModusSchreiben(_gewaehlt.ID, monat)),
                ["RegelNeu"] = EventCallback.Factory.Create(new object(), (Action)RegelNeu),
                ["RegelAbschalten"] = new Func<UmrechnungsregelZeile, bool, bool>(DarfAbschalten),
                ["InArbeitspreis"] = EventCallback.Factory.Create(new object(), (Action)ArbeitspreisAusBestandteilen),
                ["AufschlagAnwenden"] = EventCallback.Factory.Create<bool>(new object(), AufschlagAnwenden),
                ["Speichern"] = new Func<bool>(Speichern),
                ["SpeichernGrund"] = new Func<string>(SpeichernGrund),
                ["UnterdialogGeschlossen"] = EventCallback.Factory.Create(new object(),
                    (Action)UnterdialogGeschlossen),

                ["StammSchreiben"] = new Func<string, int?, bool>(StammSchreiben),
                ["NamensGaben"] = new Func<IReadOnlyDictionary<string, object>>(NamensGaben),
                ["TraegerNeu"] = new Func<string, int>(TraegerNeu),
                ["TraegerVariante"] = new Func<int>(TraegerVariante),
                ["TraegerLoeschen"] = new Func<ValueTuple<bool, string>>(TraegerLoeschen),
                ["InsProjekt"] = new Func<IReadOnlyList<int>, int>(InsProjekt),
                ["AusProjekt"] = new Func<ValueTuple<bool, string>>(AusProjekt),

                ["KostenprofilGaben"] = new Func<IReadOnlyDictionary<string, object>>(KostenprofilGaben),
                ["SpotpreisGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => SpotpreisImportHuelle.Gaben(_projektId)),
                ["SaisonGaben"] = new Func<IReadOnlyDictionary<string, object>>(SaisonGaben),
                ["EmissionskatalogGaben"] = new Func<string, IReadOnlyDictionary<string, object>>(
                    EmissionskatalogGaben),
                ["EmissionskatalogAuswerten"] = new Action<EmissionskatalogErgebnis>(
                    EmissionskatalogAuswerten),

                ["KarteTexte"] = KarteTexte(),
                ["AufschlagTexte"] = AufschlagTexte(),
                ["BestandteilTexte"] = BestandteilTexte(),

                ["TitelText"] = T("KDLG_ET_TITEL", "Energieträgerverwaltung"),
                ["KontextText"] = Katalogkontext
                    ? T("KDLG_ET_KONTEXT_KATALOG", "Kontext: Katalog (Stammdaten)")
                    : string.Format(CultureInfo.CurrentCulture,
                        T("KDLG_ET_KONTEXT_PROJEKT", "Kontext: Projekt {0}"), _projektId),
                ["ListenTitel"] = T("KDLG_ET_LISTE", "Energieträger"),
                ["LeerText"] = T("ETV_LEER", "Bitte einen Energieträger wählen."),
                ["NeuText"] = T("KDLG_ET_BTN_NEU", "Neu…"),
                ["VarianteText"] = T("KDLG_ET_BTN_VARIANTE", "Variante"),
                ["LoeschenText"] = T("KDLG_ET_BTN_LOESCHEN", "Löschen"),
                ["UebernehmenText"] = T("KDLG_ET_BTN_UEBERNEHMEN", "Aus Katalog übernehmen…"),
                ["UebernehmenKurzText"] = T("KDLG_ET_UEBERNAHME_OK", "Übernehmen"),
                ["EntfernenText"] = T("KDLG_ET_BTN_ENTFERNEN", "Entfernen"),
                ["LabelStammName"] = T("KDLG_ET_STAMM_NAME", "Bezeichnung:"),
                ["LabelStammGruppe"] = T("KDLG_ET_STAMM_GRUPPE", "Gruppe:"),
                ["StammSpeichernText"] = T("KDLG_ET_STAMM_SPEICHERN", "Übernehmen"),
                ["KarteProfilTitel"] = T("KPROF_KARTE_PROFIL_TITEL", "Kostenprofil"),
                ["KarteProfilInfo"] = T("KPROF_KARTE_PROFIL_INFO",
                    "Monatliche Preisniveaus des Strombezugs pflegen."),
                ["KarteSpotTitel"] = T("KPROF_KARTE_SPOT_TITEL", "Spotmarktpreise"),
                ["KarteSpotInfo"] = T("KPROF_KARTE_SPOT_INFO",
                    "Stundenpreise importieren und verwalten."),
                ["NeuTitel"] = T("KDLG_ET_NEU_TITEL", "Neuer Energieträger"),
                ["UebernahmeTitel"] = T("KDLG_ET_UEBERNAHME_TITEL", "Aus Katalog übernehmen"),
                ["UebernahmeFrage"] = T("ETV_UEBERNAHME_FRAGE",
                    "Welche Katalogträger sollen ins Projekt?"),
                ["UebernahmeLeer"] = T("KDLG_ET_UEBERNAHME_LEER",
                    "Alle Katalogträger sind dem Projekt bereits zugeordnet."),
                ["VorlageLoeschen"] = T("KDLG_ET_LOESCHEN_FRAGE", "Energieträger „{0}\" löschen?"),
                ["VorlageEntfernen"] = T("KDLG_ET_ENTFERNEN_FRAGE",
                    "Träger „{0}\" aus dem Projekt entfernen? (Der Katalogeintrag bleibt.)"),
                ["VorlageGesperrt"] = T("KDLG_ET_LOESCHEN_GESPERRT",
                    "Der Träger wird verwendet und bleibt erhalten: {0}"),
                ["MeldungStammLeer"] = T("KDLG_ET_STAMM_FEHLER", "Bezeichnung darf nicht leer sein."),
                ["TitelKostenprofil"] = MyResource.Resource.PREIS_PROFIL_TITEL,
                ["TitelSpotpreis"] = MyResource.Resource.PREIS_IMPORT_TITEL,
                ["TitelSaison"] = T("KDLG_LPR_TITEL", "Saisonale Leistungspreise"),
                ["TitelEmissionskatalog"] = T("EMK_TITEL", "Emissionsfaktor-Katalog"),
                ["SpeichernText"] = T("KDLG_BTN_SPEICHERN", "Speichern"),
                ["AbbrechenText"] = T("KDLG_ET_ABBRECHEN", "Abbrechen"),
                ["OkText"] = T("KDLG_BTN_OK", "OK"),
                ["JaText"] = T("KKOMP_BTN_JA", "Ja"),
                ["NeinText"] = T("KKOMP_BTN_NEIN", "Nein"),
                ["VorlageGespeichert"] = " — " + T("KDLG_GESPEICHERT", "gespeichert {0:HH:mm} Uhr")
                    .Replace("{0:HH:mm}", "{0}"),

                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), ok =>
                {
                    if (dlg != null) dlg.Schliessen(ok);
                })
            };

            dlg = new BlazorDialogForm<EnergietraegerDialog>(
                T("KDLG_ET_TITEL", "Energieträgerverwaltung"), FENSTER, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
        }

        // =====================================================================
        // Trägerliste (Ä13)
        // =====================================================================

        /// <summary>
        /// Die Träger unter ihren Gruppen — wortgleich aus
        /// <c>Form_Energietraeger.SetControls</c>: sortiert nach Gruppe, dann
        /// Name; Köpfe sind nicht wählbar.
        /// </summary>
        private void ListeLaden()
        {
            _traeger = KostenSummenCtrl.GetAllCarriers(_projektId);
            _traeger.Sort((a, b) =>
            {
                int g = string.Compare(a.GroupCode ?? "", b.GroupCode ?? "",
                                       StringComparison.CurrentCultureIgnoreCase);
                return g != 0 ? g : string.Compare(a.Name, b.Name,
                                       StringComparison.CurrentCultureIgnoreCase);
            });
        }

        private IReadOnlyList<EnergietraegerDialog.EnergietraegerListe> Listeneintraege()
        {
            var liste = new List<EnergietraegerDialog.EnergietraegerListe>();
            string gruppe = null;
            foreach (EnergyCarrier c in _traeger)
            {
                string g = string.IsNullOrEmpty(c.GroupCode)
                    ? T("KDLG_ET_GRUPPE_SONSTIGE", "Sonstige") : c.GroupCode;
                if (!string.Equals(g, gruppe, StringComparison.CurrentCultureIgnoreCase))
                {
                    gruppe = g;
                    liste.Add(new EnergietraegerDialog.EnergietraegerListe(null, g));
                }
                liste.Add(new EnergietraegerDialog.EnergietraegerListe(c.ID, c.Name));
            }
            return liste;
        }

        private IReadOnlyList<ValueTuple<int, string>> Gruppen()
        {
            var liste = new List<ValueTuple<int, string>>();
            int n = 0;
            foreach (string g in EnergietraegerKatalogCtrl.Gruppen())
                liste.Add(new ValueTuple<int, string>(n++, g));
            return liste;
        }

        private string GruppenName(int? id)
        {
            int n = 0;
            foreach (string g in EnergietraegerKatalogCtrl.Gruppen())
            {
                if (id.HasValue && n == id.Value) return g;
                n++;
            }
            return "";
        }

        // =====================================================================
        // Trägerkarte laden (wortgleich aus ucFuelSettings.LoadData)
        // =====================================================================

        /// <summary>Lädt die Trägerkarte und gibt die fertige Ansicht zurück.</summary>
        private EnergietraegerAnsicht TraegerLaden(int id)
        {
            TraegerWaehlen(id);
            return Ansicht();
        }

        /// <summary>
        /// Was die Komponente zeigt: der Stand und alles, was hier daraus
        /// gerechnet wurde — Summenzeilen der beiden Preisblöcke, der
        /// Arbeitspreis in ct/kWh, die Schnellwahlsätze und die Kartenstatus.
        /// </summary>
        private EnergietraegerAnsicht Ansicht()
        {
            var a = new EnergietraegerAnsicht { Stand = _stand };
            if (_stand == null || _gewaehlt == null) return a;

            a.ArbeitspreisCtKwh = ArbeitspreisCtKwh();
            a.StammName = _gewaehlt.Name ?? "";
            a.StammGruppe = GruppenIndex(_gewaehlt.GroupCode);

            if (_stand.Aufschlaege != null && _aufschlagModell != null)
            {
                InStromModell(_stand.Aufschlaege, _aufschlagModell);
                SpeicherEngine.Aufschlagssatz satz =
                    StromAufschlagCtrl.AlsAufschlagssatz(_aufschlagModell);
                a.AufschlagAnzeige = new PreisblockAnzeige(
                    string.Format(MyResource.Resource.PREIS_SUMME_AKTIV,
                                  Anzeige(satz.SummeAktivCtKwh), Anzeige(satz.WirksamCtKwh)),
                    _stand.Aufschlaege.Aufgeschluesselt
                        ? MyResource.Resource.PREIS_REST_HINWEIS_MODUS
                        : string.Format(MyResource.Resource.PREIS_REST_NICHT_AUFGESCHLUESSELT,
                                        Anzeige(satz.NichtAufgeschluesselterRestCtKwh)),
                    satz.NichtAufgeschluesselterRestCtKwh < 0.0);

                // Ä16: Bezugspreis = Arbeitspreis + wirksamer Aufschlag.
                double arbeitCt = _stand.Arbeitspreis * 100.0;
                _stand.EffektivpreisText = string.Format(
                    T("KDLG_EFFEKTIVPREIS",
                      "Bezugspreis inkl. Aufschläge: {0:N2} ct/kWh  (Arbeitspreis {1:N2} + Aufschlag {2:N2})"),
                    arbeitCt + satz.WirksamCtKwh, arbeitCt, satz.WirksamCtKwh);

                a.SatzRegelfall = StromsteuerSatz(DbWerte.GESETZ_STROMST_REGELSATZ,
                    StromAufschlagModel.STROMSTEUER_REGELFALL,
                    !ReduzierterSatzEmpfohlen());
                a.SatzReduziert = StromsteuerSatz(DbWerte.GESETZ_STROMST_REDUZIERT,
                    StromAufschlagModel.STROMSTEUER_REDUZIERT,
                    ReduzierterSatzEmpfohlen());
            }

            if (_stand.Bestandteile != null && _bestandteilModell != null)
            {
                InBrennstoffModell(_stand.Bestandteile, _bestandteilModell);
                double summe = BrennstoffBestandteilCtrl
                    .AlsAufschlagssatz(_bestandteilModell).SummeAktivCtKwh;
                bool aufgeschluesselt = _stand.Bestandteile.Aufgeschluesselt;
                double rest = a.ArbeitspreisCtKwh - summe;

                a.BestandteilAnzeige = new PreisblockAnzeige(
                    string.Format(aufgeschluesselt
                            ? T("BB_PREIS_AUS_BESTANDTEILEN", "Preis aus den Bestandteilen: {0} ct/kWh")
                            : T("BB_SUMME_AKTIV", "Summe der aktiven Bestandteile: {0} ct/kWh"),
                        Anzeige(summe)),
                    aufgeschluesselt
                        ? T("BB_REST_HINWEIS_MODUS",
                            "Im Modus „aufgeschlüsselt\" ist die Summe der Bestandteile der Preis. "
                            + "Der Arbeitspreis ändert sich erst, wenn Sie ihn übernehmen.")
                        : string.Format(T("BB_REST", "Nicht aufgeschlüsselter Rest: {0} ct/kWh"),
                                        Anzeige(rest)),
                    !aufgeschluesselt && rest < 0.0);

                a.SatzRegel = EnergiesteuerSatz(
                    WirtschaftlichkeitCtrl.EnergiesteuerSchluessel(_idBrennstoff, false),
                    T("BB_BTN_SATZ_REGEL", "§ 2: {0}"));
                a.Satz53a = EnergiesteuerSatz(
                    WirtschaftlichkeitCtrl.EnergiesteuerSchluessel(_idBrennstoff, true),
                    T("BB_BTN_SATZ_53A", "§ 53a: {0}"));
                a.Satz54 = EnergiesteuerSatz(
                    WirtschaftlichkeitCtrl.Energiesteuer54Schluessel(_idBrennstoff),
                    T("BB_BTN_SATZ_54", "§ 54: {0}"));
                a.SatzCo2 = Co2Satz();
            }

            bool strom = string.Equals(_gewaehlt.PricingModel, "ELECTRICITY",
                                       StringComparison.OrdinalIgnoreCase);
            a.MitStromkarten = strom;
            a.MitKostenprofil = strom && _projektId > 0;
            if (strom) KartenStatus(a);

            return a;
        }

        private int? GruppenIndex(string gruppe)
        {
            int n = 0;
            foreach (string g in EnergietraegerKatalogCtrl.Gruppen())
            {
                if (string.Equals(g, gruppe ?? "", StringComparison.Ordinal)) return n;
                n++;
            }
            return null;
        }

        private static string Anzeige(double wert)
        {
            return wert.ToString("0.###", CultureInfo.CurrentCulture);
        }

        private void TraegerWaehlen(int id)
        {
            _gewaehlt = null;
            foreach (EnergyCarrier c in _traeger)
                if (c.ID == id) { _gewaehlt = c; break; }
            if (_gewaehlt == null) { _stand = null; return; }

            _umrechnungen = EnergietraegerPreisCtrl.Umrechnungen(_gewaehlt.ID_Brennstoff);
            _idBrennstoff = _gewaehlt.ID_Brennstoff;
            _abrechnungseinheit = _gewaehlt.BillingUnit ?? "";

            var stand = new EnergietraegerStand
            {
                TraegerZeile = _gewaehlt.Name + "  (VDI 3805 " + _gewaehlt.Code + ")",
                GruppeZeile = "Gruppe: " + _gewaehlt.GroupCode,
                MitHeizwert = _gewaehlt.HasHi,
                MitBrennwert = _gewaehlt.HasHs,
                MitLeistungspreis = _gewaehlt.HasPowerPrice,
                MitFormel = _gewaehlt.HasHi,
                Basiseinheit = _gewaehlt.BillingUnit ?? "",
                GueltigAb = DateOnly.FromDateTime(DateTime.Now),
                EinheitGrundpreis = "€/a"
            };

            var basen = new List<ValueTuple<int, string>>();
            for (int i = 0; i < _umrechnungen.Count; i++)
                basen.Add(new ValueTuple<int, string>(i, _umrechnungen[i].ToUnitCode));
            stand.Preisbasen = basen;

            EnergietraegerPreisCtrl.Projektpreis projekt =
                EnergietraegerPreisCtrl.ProjektpreisLesen(_projektId, _gewaehlt.ID);

            if (projekt != null)
            {
                // Ä-BK3: Die drei Preisspalten dürfen NULL sein — Rückfall ist
                // derselbe Katalogwert, den auch der else-Zweig setzt.
                stand.Arbeitspreis = projekt.Arbeitspreis ?? _gewaehlt.price_work;
                stand.Grundpreis = projekt.Grundpreis ?? _gewaehlt.price_base;
                stand.Leistungspreis = projekt.Leistungspreis ?? _gewaehlt.price_power;
                stand.Heizwert = projekt.Hi ?? _gewaehlt.HiKwhPerUnit;
                stand.Brennwert = projekt.Hs ?? _gewaehlt.HsKwhPerUnit;
                stand.AltCO2 = projekt.CO2 ?? _gewaehlt.CO2;
                stand.AltSO2 = projekt.SO2 ?? _gewaehlt.SO2;
                stand.AltNOx = projekt.NOx ?? _gewaehlt.NOx;

                _baseHi = stand.Heizwert;
                _baseHs = stand.Brennwert;

                int idUmrechnung = projekt.IdUmrechnung ?? -1;
                string ziel = idUmrechnung > 0 ? EnergietraegerPreisCtrl.Zieleinheit(idUmrechnung) : null;
                stand.PreisbasisId = IndexZuEinheit(ziel);
            }
            else
            {
                stand.Arbeitspreis = _gewaehlt.price_work;
                stand.Grundpreis = _gewaehlt.price_base;
                stand.Leistungspreis = _gewaehlt.price_power;
                stand.Heizwert = _gewaehlt.HiKwhPerUnit;
                stand.Brennwert = _gewaehlt.HsKwhPerUnit;
                stand.AltCO2 = _gewaehlt.CO2;
                stand.AltSO2 = _gewaehlt.SO2;
                stand.AltNOx = _gewaehlt.NOx;

                _baseHi = stand.Heizwert;
                _baseHs = stand.Brennwert;
                stand.PreisbasisId = IndexZuEinheit(_gewaehlt.BillingUnit);
            }

            _baseWork = stand.Arbeitspreis;
            _basePower = stand.Leistungspreis;
            _baseGround = stand.Grundpreis;

            _dbWork = _baseWork; _dbGround = _baseGround; _dbPower = _basePower;
            _dbHi = _baseHi; _dbHs = _baseHs;
            _dbCO2 = stand.AltCO2; _dbSO2 = stand.AltSO2; _dbNOx = stand.AltNOx;

            stand.LeistungsModusMonat = string.Equals(
                EnergietraegerPreisCtrl.LeistungsModus(_gewaehlt.ID),
                DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal);

            _regeln = EnergieEinheitenPruefung.RegelnDesBrennstoffs(_gewaehlt.ID_Brennstoff);
            _stand = stand;
            stand.ReihenStatus = ReihenStatus();

            BloeckeAufbauen();
            EmissionenAufbauen();
            Nachziehen();
        }

        private int? IndexZuEinheit(string einheit)
        {
            if (string.IsNullOrEmpty(einheit)) return _umrechnungen.Count > 0 ? (int?)0 : null;
            for (int i = 0; i < _umrechnungen.Count; i++)
                if (string.Equals(_umrechnungen[i].ToUnitCode, einheit, StringComparison.Ordinal))
                    return i;
            return _umrechnungen.Count > 0 ? (int?)0 : null;
        }

        private EnergyConversion AktuelleUmrechnung()
        {
            int? i = _stand != null ? _stand.PreisbasisId : null;
            return i.HasValue && i.Value >= 0 && i.Value < _umrechnungen.Count
                ? _umrechnungen[i.Value] : null;
        }

        private string AktuelleEinheit()
        {
            EnergyConversion c = AktuelleUmrechnung();
            return c != null && !string.IsNullOrEmpty(c.ToUnitCode)
                ? c.ToUnitCode : (_gewaehlt != null ? _gewaehlt.BillingUnit ?? "" : "");
        }

        // =====================================================================
        // Die beiden Preisblöcke (AP4 / B2)
        // =====================================================================

        private void BloeckeAufbauen()
        {
            _aufschlagModell = null;
            _bestandteilModell = null;
            if (_gewaehlt == null || _stand == null) return;

            string modell = (_gewaehlt.PricingModel ?? "").ToUpperInvariant();

            if (string.Equals(modell, StromAufschlagCtrl.PRICING_MODEL_STROM,
                              StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    StromAufschlagCtrl.StelleSpaltenSicher();
                    _aufschlagModell = new StromAufschlagCtrl().Read(_projektId, _gewaehlt.ID);
                    _stand.Aufschlaege = AusStromModell(_aufschlagModell);
                    _stand.MitAufschlagSchalter = _projektId > 0;
                    if (_projektId > 0)
                    {
                        try
                        {
                            _stand.AufschlaegeAnwenden = new WirtschaftlichkeitCtrl()
                                .LadeParameter(_projektId).AufschlaegeAnwenden;
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    // Ein fehlender Aufschlagsblock darf die Preispflege nicht
                    // blockieren — etwa ohne Migrationsschritt 12.
                    Console.WriteLine("Der Aufschlagsblock konnte nicht aufgebaut werden: " + ex.Message);
                    _aufschlagModell = null;
                }
                return;
            }

            if (Array.IndexOf(PREISMODELLE_BRENNSTOFF, modell) < 0) return;

            try
            {
                BrennstoffBestandteilCtrl.StelleSpaltenSicher();
                _bestandteilModell = new BrennstoffBestandteilCtrl().Read(_projektId, _gewaehlt.ID);
                _stand.Bestandteile = AusBrennstoffModell(_bestandteilModell);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Der Zerlegungsblock konnte nicht aufgebaut werden: " + ex.Message);
                _bestandteilModell = null;
            }
        }

        /// <summary>
        /// Preismodelle, deren Träger eine Preiszerlegung nach § 4.1 bekommen —
        /// wortgleich aus <c>ucFuelSettings.PREISMODELLE_BRENNSTOFF</c>.
        /// </summary>
        private static readonly string[] PREISMODELLE_BRENNSTOFF =
        {
            "GASEOUS_FUEL", "LIQUID_FUEL", "SOLID_FUEL", "ANIMAL_FAT"
        };

        private static StromAufschlaegeStand AusStromModell(StromAufschlagModel m)
        {
            return new StromAufschlaegeStand
            {
                Aufgeschluesselt = m.Modus != DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT,
                Netzentgelt = m.Netzentgelt, NetzentgeltAktiv = m.Netzentgelt_Aktiv,
                Umlagen = m.Umlagen, UmlagenAktiv = m.Umlagen_Aktiv,
                Stromsteuer = m.Stromsteuer, StromsteuerAktiv = m.Stromsteuer_Aktiv,
                Konzession = m.Konzession, KonzessionAktiv = m.Konzession_Aktiv,
                Vertrieb = m.Vertrieb, VertriebAktiv = m.Vertrieb_Aktiv,
                Override = m.Override,
                VerguetungPv = m.Verguetung_PV,
                VerguetungBhkw = m.Verguetung_BHKW
            };
        }

        private static void InStromModell(StromAufschlaegeStand s, StromAufschlagModel m)
        {
            m.Netzentgelt = s.Netzentgelt; m.Netzentgelt_Aktiv = s.NetzentgeltAktiv;
            m.Umlagen = s.Umlagen; m.Umlagen_Aktiv = s.UmlagenAktiv;
            m.Stromsteuer = s.Stromsteuer; m.Stromsteuer_Aktiv = s.StromsteuerAktiv;
            m.Konzession = s.Konzession; m.Konzession_Aktiv = s.KonzessionAktiv;
            m.Vertrieb = s.Vertrieb; m.Vertrieb_Aktiv = s.VertriebAktiv;
            m.Override = s.Override;
            m.Verguetung_PV = s.VerguetungPv;
            m.Verguetung_BHKW = s.VerguetungBhkw;
            m.Modus = s.Aufgeschluesselt
                ? DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT
                : DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT;
        }

        private static BrennstoffBestandteileStand AusBrennstoffModell(BrennstoffBestandteilModel m)
        {
            return new BrennstoffBestandteileStand
            {
                Aufgeschluesselt = m.Modus == DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT,
                Energiesteuer = m.Energiesteuer, EnergiesteuerAktiv = m.Energiesteuer_Aktiv,
                CO2 = m.CO2, CO2Aktiv = m.CO2_Aktiv,
                Netzentgelt = m.Netzentgelt, NetzentgeltAktiv = m.Netzentgelt_Aktiv,
                Vertrieb = m.Vertrieb, VertriebAktiv = m.Vertrieb_Aktiv
            };
        }

        private static void InBrennstoffModell(BrennstoffBestandteileStand s,
                                               BrennstoffBestandteilModel m)
        {
            m.Energiesteuer = s.Energiesteuer; m.Energiesteuer_Aktiv = s.EnergiesteuerAktiv;
            m.CO2 = s.CO2; m.CO2_Aktiv = s.CO2Aktiv;
            m.Netzentgelt = s.Netzentgelt; m.Netzentgelt_Aktiv = s.NetzentgeltAktiv;
            m.Vertrieb = s.Vertrieb; m.Vertrieb_Aktiv = s.VertriebAktiv;
            m.Modus = s.Aufgeschluesselt
                ? DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT
                : DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT;
        }

        // =====================================================================
        // Emissionen (Etappe E3)
        // =====================================================================

        private void EmissionenAufbauen()
        {
            if (_gewaehlt == null || _stand == null) return;
            try
            {
                _emissionen = new EmissionenCtrl(_projektId, _gewaehlt.ID);
                _emissionen.Laden();
                EmissionszeilenSetzen();
            }
            catch (Exception ex)
            {
                // Ein misslungener Emissionsteil darf die Preispflege nicht
                // blockieren — dieselbe Zusage wie beim Umrechnungsblock.
                Console.WriteLine("Der Emissionsteil konnte nicht aufgebaut werden: " + ex.Message);
                _emissionen = null;
                _stand.EmissionenVerfuegbar = false;
            }
        }

        private void EmissionszeilenSetzen()
        {
            if (_emissionen == null || _stand == null) return;

            _stand.EmissionenVerfuegbar = _emissionen.Verfuegbar;
            _stand.ModusCo2e = string.Equals(_emissionen.Modus, DbWerte.EMISSION_MODUS_CO2E,
                                             StringComparison.Ordinal);
            _stand.ModusOrt = _projektId > 0
                ? T("KDLG_EM_MODUS_ORT_PROJEKT", "[Projekt]")
                : T("KDLG_EM_MODUS_ORT_VORGABE", "[globale Vorgabe]");

            if (!_emissionen.Verfuegbar)
            {
                _stand.Emissionszeilen = Array.Empty<EmissionsFeldZeile>();
                return;
            }

            var zeilen = new List<EmissionsFeldZeile>();
            foreach (EmissionsZeile z in _emissionen.Zeilen)
            {
                zeilen.Add(new EmissionsFeldZeile
                {
                    Kuerzel = z.Kuerzel,
                    Name = z.Art.Name,
                    Einheit = z.Art.Einheit,
                    Wert = z.Wert,
                    Herkunft = z.QuelleText,
                    NurLesend = z.NurLesend
                });
            }
            _stand.Emissionszeilen = zeilen;
            EmissionsSummeSetzen();
        }

        private void EmissionsSummeSetzen()
        {
            if (_emissionen == null || _stand == null || !_emissionen.Verfuegbar) return;

            _stand.EmissionsSumme = string.Format(CultureInfo.CurrentCulture,
                T("KDLG_EM_SUMME", "CO₂-Äquivalent gesamt (ausgewählte Arten): {0} g/kWh"),
                _emissionen.SummeCo2eGKwh().ToString("N2", CultureInfo.CurrentCulture));

            _stand.EmissionsHinweis = _emissionen.SummeIstBereitsAequivalent()
                ? T("KDLG_EM_SUMME_F3",
                    "CO₂-Wert ist bereits Äquivalent — Summe = Wert, weitere Arten werden "
                    + "nicht aufsummiert.")
                : "";
        }

        /// <summary>
        /// Konzept F9: Die Rechner lesen bis Etappe E5 die alten Spalten — eine
        /// Kernart wird deshalb in ihr Bestandsfeld gespiegelt.
        /// </summary>
        private void KernwerteSpiegeln()
        {
            if (_emissionen == null || _stand == null || !_emissionen.Verfuegbar) return;
            foreach (EmissionsZeile z in _emissionen.Zeilen)
            {
                double wert = z.Wert ?? 0.0;
                if (string.Equals(z.Kuerzel, DbWerte.EMISSIONSART_CO2, StringComparison.OrdinalIgnoreCase))
                    _stand.AltCO2 = wert;
                else if (string.Equals(z.Kuerzel, DbWerte.EMISSIONSART_SO2, StringComparison.OrdinalIgnoreCase))
                    _stand.AltSO2 = wert;
                else if (string.Equals(z.Kuerzel, DbWerte.EMISSIONSART_NOX, StringComparison.OrdinalIgnoreCase))
                    _stand.AltNOx = wert;
            }
        }

        // =====================================================================
        // Nachziehen: Einheiten, Formel, Effektivzeile, Blöcke
        // =====================================================================

        /// <summary>
        /// Nach jeder Feldänderung — der Sammelweg der Methoden
        /// <c>UpdatePricePerKWh</c>, <c>AktualisiereEffektivUndVerstoss</c>,
        /// <c>BestandteileNachziehen</c> und <c>EffektivpreisAnzeigen</c>.
        /// </summary>
        private void Nachziehen()
        {
            if (_stand == null || _gewaehlt == null) return;

            EnergyConversion conv = AktuelleUmrechnung();
            string einheit = conv != null ? conv.ToUnitCode : _stand.Basiseinheit;

            _stand.EinheitArbeitspreis = _gewaehlt.HasHi ? "€/" + einheit : "€ / kWh";
            _stand.EinheitHeizwert = "kWh/" + einheit;
            _stand.EinheitBrennwert = "kWh/" + einheit;
            _stand.EinheitLeistungspreis = _stand.LeistungsModusMonat ? "€/(kW·Monat)" : "€/(kW·a)";

            // Basiswerte aus den Anzeigewerten (der Vorläufer hielt sie in den
            // Value-Changed-Handlern nach).
            double faktor = conv != null ? conv.Factor : 1.0;
            _baseWork = _gewaehlt.HasHi ? _stand.Arbeitspreis * faktor : _stand.Arbeitspreis;
            _baseHi = _stand.Heizwert * faktor;
            _baseHs = _stand.Brennwert * faktor;
            _basePower = _gewaehlt.HasPowerPrice ? _stand.Leistungspreis * faktor : _stand.Leistungspreis;
            _baseGround = _stand.Grundpreis;

            FormelSetzen();
            EffektivSetzen();
            RegelnSetzen();
            EmissionsSummeSetzen();
        }

        /// <summary>Wortgleich aus <c>UpdatePricePerKWh</c>.</summary>
        private void FormelSetzen()
        {
            if (!_gewaehlt.HasHi)
            {
                _stand.PreisJeKwh = _stand.Arbeitspreis.ToString("N4", CultureInfo.CurrentCulture) + " €";
                _stand.FormelText = "Direktabrechnung nach kWh";
                return;
            }
            if (_stand.Heizwert <= 0) return;

            double ergebnis = _stand.Arbeitspreis / _stand.Heizwert;
            _stand.PreisJeKwh = ergebnis.ToString("N4", CultureInfo.CurrentCulture) + " €";
            _stand.FormelText =
                _stand.Arbeitspreis.ToString("N2", CultureInfo.CurrentCulture) + " € ÷ " +
                _stand.Heizwert.ToString("N2", CultureInfo.CurrentCulture) + " kWh = " +
                ergebnis.ToString("N4", CultureInfo.CurrentCulture) + " €/kWh";
        }

        /// <summary>Wortgleich aus <c>AktualisiereEffektivUndVerstoss</c>.</summary>
        private void EffektivSetzen()
        {
            string einheit = AktuelleEinheit();
            double hi = _stand.Heizwert;
            double hs = _stand.Brennwert;

            _stand.EffektivText = string.Equals(einheit, DbWerte.EINHEIT_KWH,
                                                StringComparison.OrdinalIgnoreCase)
                ? MyResource.Resource.KOSTEN_UMRECHNUNG_EFFEKTIV_KWH
                : string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOSTEN_UMRECHNUNG_EFFEKTIV, einheit,
                    hi.ToString("N2", CultureInfo.CurrentCulture),
                    hs.ToString("N2", CultureInfo.CurrentCulture));

            string grund;
            _stand.VerstossText = EnergieEinheitenPruefung.ErreichtKwh(einheit, hi, hs, _regeln, out grund)
                ? "" : grund;
        }

        private void RegelnSetzen()
        {
            var zeilen = new List<UmrechnungsregelZeile>();
            for (int i = 0; i < _regeln.Count; i++)
            {
                UmrechnungsRegel r = _regeln[i];
                zeilen.Add(new UmrechnungsregelZeile
                {
                    Nummer = i,
                    Name = r.Name ?? "",
                    Von = r.Von ?? "",
                    Nach = r.Nach ?? "",
                    Faktor = r.Faktor,
                    Aktiv = r.Aktiv
                });
            }
            _stand.Regeln = zeilen;
        }

        /// <summary>Übernimmt die Anzeigezeilen in die Speicherkopie (K3, L5).</summary>
        private void RegelnUebernehmen()
        {
            foreach (UmrechnungsregelZeile z in _stand.Regeln)
            {
                if (z.Nummer < 0 || z.Nummer >= _regeln.Count) continue;
                UmrechnungsRegel r = _regeln[z.Nummer];
                bool geaendert = !string.Equals(r.Name ?? "", z.Name, StringComparison.Ordinal) ||
                                 !string.Equals(r.Von ?? "", z.Von, StringComparison.Ordinal) ||
                                 !string.Equals(r.Nach ?? "", z.Nach, StringComparison.Ordinal) ||
                                 Math.Abs(r.Faktor - z.Faktor) > 1e-12 || r.Aktiv != z.Aktiv;
                r.Name = z.Name;
                r.Von = z.Von;
                r.Nach = z.Nach;
                r.Faktor = z.Faktor;
                r.Aktiv = z.Aktiv;
                // Jede Handänderung macht die Zeile zu einer gepflegten — ab dann
                // fasst sie keine Migration mehr an (L5).
                if (geaendert) r.UserEdited = true;
            }
        }

        /// <summary>Wortgleich aus <c>BtnRegelNeu_Click</c>.</summary>
        private void RegelNeu()
        {
            if (_gewaehlt == null) return;
            RegelnUebernehmen();
            bool gas = string.Equals(_gewaehlt.PricingModel, "GASEOUS_FUEL",
                                     StringComparison.OrdinalIgnoreCase);
            _regeln.Add(new UmrechnungsRegel
            {
                Id = 0,
                IdBrennstoff = _gewaehlt.ID_Brennstoff,
                Name = gas ? DbWerte.UMRECHNUNG_NAME_Z_FAKTOR : DbWerte.UMRECHNUNG_NAME_STANDARD,
                Von = AktuelleEinheit(),
                Nach = "",
                Faktor = 1,
                Aktiv = true,
                UserEdited = true
            });
            RegelnSetzen();
            EffektivSetzen();
        }

        /// <summary>
        /// DER RIEGEL (§ 4.3): Die Regel, die den Träger nach kWh trägt, lässt
        /// sich nicht abschalten. Gefragt wird der Prüfer — es gibt keine
        /// zweite Fassung der Fachregel.
        /// </summary>
        private bool DarfAbschalten(UmrechnungsregelZeile zeile, bool neu)
        {
            if (neu || _stand == null) return true;
            RegelnUebernehmen();

            string grund;
            if (EnergieEinheitenPruefung.DarfAbschalten(AktuelleEinheit(), _stand.Heizwert,
                                                        _stand.Brennwert, _regeln, zeile.Nummer,
                                                        out grund))
                return true;

            _stand.VerstossText = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KOSTEN_UMRECHNUNG_RIEGEL, grund);
            return false;
        }

        private void PreisbasisWechseln(int index)
        {
            if (_stand == null || index < 0 || index >= _umrechnungen.Count) return;

            // Wortgleich aus CmbUnit_SelectedIndexChanged: die Basiswerte bleiben,
            // die Anzeigewerte werden umgerechnet.
            EnergyConversion conv = _umrechnungen[index];
            _stand.PreisbasisId = index;
            _stand.Arbeitspreis = _baseWork / conv.Factor;
            _stand.Heizwert = _baseHi / conv.Factor;
            if (_gewaehlt.HasPowerPrice) _stand.Leistungspreis = _basePower / conv.Factor;
            if (_gewaehlt.HasHs) _stand.Brennwert = _baseHs / conv.Factor;

            Nachziehen();
        }

        private void AufschlagAnwenden(bool an)
        {
            if (_projektId <= 0) return;
            try
            {
                var ctrl = new WirtschaftlichkeitCtrl();
                WirtschaftlichkeitParameter p = ctrl.LadeParameter(_projektId);
                p.AufschlaegeAnwenden = an;
                ctrl.SpeichereParameter(p);
            }
            catch { }
        }

        /// <summary>
        /// Trägt den Preis aus den Bestandteilen in das Arbeitspreisfeld ein —
        /// der Rückweg von ct/kWh in die Abrechnungseinheit (wortgleich aus
        /// <c>ArbeitspreisAusBestandteilen</c>). Ohne Heizwert gibt es keinen
        /// Rückweg; dann bleibt das Feld, wie es war.
        /// </summary>
        private void ArbeitspreisAusBestandteilen()
        {
            if (_stand == null || _stand.Bestandteile == null || _bestandteilModell == null) return;
            InBrennstoffModell(_stand.Bestandteile, _bestandteilModell);

            double ctKwh = BrennstoffBestandteilCtrl.AlsAufschlagssatz(_bestandteilModell).SummeAktivCtKwh;
            double jeEinheit;

            if (!_gewaehlt.HasHi) jeEinheit = ctKwh / 100.0;
            else
            {
                double hi = _stand.Heizwert;
                if (hi <= 0.0) return;
                jeEinheit = ctKwh / 100.0 * hi;
            }

            _stand.Arbeitspreis = jeEinheit;
            Nachziehen();
        }

        /// <summary>Der Arbeitspreis in ct/kWh — wortgleich aus <c>ArbeitspreisInCtKwh</c>.</summary>
        private double ArbeitspreisCtKwh()
        {
            try
            {
                if (_gewaehlt == null || !_gewaehlt.HasHi) return _baseWork * 100.0;
                if (_baseHi <= 0.0) return 0.0;
                return _baseWork / _baseHi * 100.0;
            }
            catch { return 0.0; }
        }

        // =====================================================================
        // Speichern (Ä14)
        // =====================================================================

        private string _speichernGrund = "";

        private string SpeichernGrund() { return _speichernGrund; }

        /// <summary>
        /// Ä14: „OK" und „Speichern" schreiben die offene Trägerkarte. Im
        /// Projektkontext nur, wenn der Träger dem Projekt zugeordnet ist; im
        /// Katalogkontext über den Katalogzweig (Ä9).
        /// </summary>
        private bool Speichern()
        {
            _speichernGrund = "";
            if (_stand == null || _gewaehlt == null) return true;

            // ETAPPE K3: die BLOCKIERENDE Prüfung. Der Träger muss kWh erreichen.
            string grund;
            if (!EnergieEinheitenPruefung.ErreichtKwh(AktuelleEinheit(), _stand.Heizwert,
                                                      _stand.Brennwert, _regeln, out grund))
            {
                _stand.VerstossText = grund;
                _speichernGrund = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOSTEN_UMRECHNUNG_SPEICHERN_ABGELEHNT, grund);
                return false;
            }

            if (_projektId > 0 && !EnergietraegerPreisCtrl.ImProjekt(_projektId, _gewaehlt.ID))
                return true;   // nicht zugeordnet: nichts schreiben (Bestandsverhalten)

            try
            {
                RegelnUebernehmen();
                EnergietraegerPreisCtrl.RegelnSpeichern(_regeln);
                WerteSpeichern();
                EmissionenSpeichern();
            }
            catch (Exception ex)
            {
                _speichernGrund = ex.Message;
                return false;
            }
            return true;
        }

        /// <summary>Wortgleich aus <c>SpeichereWerte</c>, nur über den Controller.</summary>
        private void WerteSpeichern()
        {
            KernwerteSpiegeln();

            var preis = new EnergietraegerPreisCtrl.Preisstand
            {
                Arbeitspreis = _baseWork,
                Grundpreis = _stand.Grundpreis,
                Leistungspreis = _stand.Leistungspreis,
                Hi = _baseHi,
                Hs = _baseHs,
                CO2 = _stand.AltCO2,
                SO2 = _stand.AltSO2,
                NOx = _stand.AltNOx,
                IdUmrechnung = EnergietraegerPreisCtrl.UmrechnungsId(AktuelleUmrechnung()),
                Basiseinheit = _stand.Basiseinheit
            };

            if (_projektId <= 0)
            {
                EnergietraegerPreisCtrl.Katalogwerte(_gewaehlt.ID, preis);

                _gewaehlt.price_work = preis.Arbeitspreis;
                _gewaehlt.price_base = preis.Grundpreis;
                _gewaehlt.price_power = preis.Leistungspreis;
                _gewaehlt.HiKwhPerUnit = preis.Hi;
                _gewaehlt.HsKwhPerUnit = preis.Hs;
                _gewaehlt.CO2 = preis.CO2;
                _gewaehlt.SO2 = preis.SO2;
                _gewaehlt.NOx = preis.NOx;

                AnkerSetzen(preis);
                return;
            }

            // Vergleich auf Basis der unberührten DB-Urwerte.
            bool geaendert = Math.Abs(preis.Arbeitspreis - _dbWork) > 0.0001 ||
                             Math.Abs(preis.Hi - _dbHi) > 0.0001 ||
                             Math.Abs(preis.Hs - _dbHs) > 0.0001 ||
                             Math.Abs(preis.Grundpreis - _dbGround) > 0.01 ||
                             Math.Abs(preis.Leistungspreis - _dbPower) > 0.01 ||
                             Math.Abs(preis.CO2 - _dbCO2) > 0.01 ||
                             Math.Abs(preis.SO2 - _dbSO2) > 0.01 ||
                             Math.Abs(preis.NOx - _dbNOx) > 0.01;

            if (geaendert)
            {
                DateTime datum = _stand.GueltigAb.HasValue
                    ? _stand.GueltigAb.Value.ToDateTime(TimeOnly.MinValue) : DateTime.Now;
                EnergietraegerPreisCtrl.HistorieSchreiben(_gewaehlt.ID, _projektId, datum, preis);
                AnkerSetzen(preis);
            }

            EnergietraegerPreisCtrl.Projektwerte(_projektId, _gewaehlt.ID, preis);

            // AP4/B2: Die beiden Blöcke schreiben in DIESELBE Zeile und deshalb
            // ERST JETZT — vor dem Upsert gäbe es beim ersten Speichern keine.
            if (_stand.Aufschlaege != null && _aufschlagModell != null)
            {
                InStromModell(_stand.Aufschlaege, _aufschlagModell);
                new StromAufschlagCtrl().Update(_aufschlagModell);
            }
            if (_stand.Bestandteile != null && _bestandteilModell != null)
            {
                InBrennstoffModell(_stand.Bestandteile, _bestandteilModell);
                new BrennstoffBestandteilCtrl().Update(_bestandteilModell);
            }
        }

        private void AnkerSetzen(EnergietraegerPreisCtrl.Preisstand p)
        {
            _dbWork = p.Arbeitspreis; _dbGround = p.Grundpreis; _dbPower = p.Leistungspreis;
            _dbHi = p.Hi; _dbHs = p.Hs;
            _dbCO2 = p.CO2; _dbSO2 = p.SO2; _dbNOx = p.NOx;
        }

        private void EmissionenSpeichern()
        {
            if (_emissionen == null || !_emissionen.Verfuegbar || _stand == null) return;
            try
            {
                foreach (EmissionsFeldZeile z in _stand.Emissionszeilen)
                {
                    if (z.NurLesend) continue;
                    EmissionsZeile ziel = ZeileZuKuerzel(z.Kuerzel);
                    if (ziel != null)
                        _emissionen.WertEingeben(ziel, z.Wert.HasValue
                            ? z.Wert.Value.ToString("0.####", CultureInfo.CurrentCulture) : "");
                }
                _emissionen.Modus = _stand.ModusCo2e
                    ? DbWerte.EMISSION_MODUS_CO2E : DbWerte.EMISSION_MODUS_CO2;
                _emissionen.Speichern();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Emissionswerte konnten nicht gespeichert werden: " + ex.Message);
            }
        }

        private EmissionsZeile ZeileZuKuerzel(string kuerzel)
        {
            if (_emissionen == null) return null;
            foreach (EmissionsZeile z in _emissionen.Zeilen)
                if (string.Equals(z.Kuerzel, kuerzel, StringComparison.OrdinalIgnoreCase)) return z;
            return null;
        }

        // =====================================================================
        // Katalogpflege (Ä9/Ä10)
        // =====================================================================

        private bool StammSchreiben(string name, int? gruppe)
        {
            if (_gewaehlt == null) return false;
            if (!EnergietraegerKatalogCtrl.Umbenennen(_gewaehlt.ID, name, GruppenName(gruppe)))
                return false;
            ListeLaden();
            return true;
        }

        private IReadOnlyDictionary<string, object> NamensGaben()
        {
            return NamensDialogHuelle.Gaben(
                T("KDLG_ET_NEU_TITEL", "Neuer Energieträger"),
                T("KDLG_ET_NEU_NAME", "Bezeichnung des neuen Trägers:"),
                T("KDLG_ET_NEU_VORGABE", "Neuer Energieträger"),
                T("NAMD_MSG_LEER", "Bitte einen Namen eingeben."));
        }

        private int TraegerNeu(string name)
        {
            int id = EnergietraegerKatalogCtrl.Neu(name, null);
            if (id > 0) { ListeLaden(); TraegerWaehlen(id); }
            return id;
        }

        private int TraegerVariante()
        {
            if (_gewaehlt == null) return 0;
            int id = EnergietraegerKatalogCtrl.Variante(_gewaehlt.ID);
            if (id > 0) { ListeLaden(); TraegerWaehlen(id); }
            return id;
        }

        private ValueTuple<bool, string> TraegerLoeschen()
        {
            if (_gewaehlt == null) return new ValueTuple<bool, string>(false, "");
            string grund;
            bool ok = EnergietraegerKatalogCtrl.Loeschen(_gewaehlt.ID, out grund);
            if (ok) { ListeLaden(); _gewaehlt = null; _stand = null; }
            return new ValueTuple<bool, string>(ok, grund ?? "");
        }

        private int InsProjekt(IReadOnlyList<int> ids)
        {
            int letzter = 0;
            foreach (int id in ids)
                if (EnergietraegerKatalogCtrl.InsProjekt(_projektId, id)) letzter = id;
            if (letzter > 0) { ListeLaden(); TraegerWaehlen(letzter); }
            return letzter;
        }

        private ValueTuple<bool, string> AusProjekt()
        {
            if (_gewaehlt == null) return new ValueTuple<bool, string>(false, "");
            string grund;
            bool ok = EnergietraegerKatalogCtrl.AusProjektEntfernen(_projektId, _gewaehlt.ID, out grund);
            if (ok) { ListeLaden(); _gewaehlt = null; _stand = null; }
            return new ValueTuple<bool, string>(ok, grund ?? "");
        }

        // =====================================================================
        // Unterdialoge
        // =====================================================================

        private IReadOnlyDictionary<string, object> KostenprofilGaben()
        {
            var ctrl = new KostenprofilCtrl();
            var vorhandene = ctrl.ReadAllByProjekt(_projektId);
            int id = vorhandene.Count > 0 ? vorhandene[0].ID : 0;
            return KostenprofilHuelle.Gaben(_projektId, id);
        }

        private IReadOnlyDictionary<string, object> SaisonGaben()
        {
            return _gewaehlt == null
                ? new Dictionary<string, object>()
                : LeistungspreisReiheHuelle.Gaben(_projektId, _gewaehlt.ID, _gewaehlt.Name);
        }

        private EmissionskatalogHuelle.Aufruf _katalogAufruf;

        private IReadOnlyDictionary<string, object> EmissionskatalogGaben(string kuerzel)
        {
            if (_gewaehlt == null) return new Dictionary<string, object>();
            _katalogAufruf = EmissionskatalogHuelle.Gaben(_gewaehlt.ID, _gewaehlt.Name,
                                                          kuerzel ?? "",
                                                          !string.IsNullOrEmpty(kuerzel));
            return _katalogAufruf.Parameter;
        }

        /// <summary>
        /// Wortgleich aus <c>KatalogFuerZeile</c>/<c>KatalogVerwalten</c>: Der
        /// übernommene Wert lebt bis zum Speichern nur im Objekt (Ä12/Ä14); die
        /// Änderungsmerker gelten auch bei „Abbrechen" (A-16 aus Welle 3).
        /// </summary>
        private void EmissionskatalogAuswerten(EmissionskatalogErgebnis ergebnis)
        {
            if (_katalogAufruf == null || _emissionen == null) return;
            EmissionskatalogHuelle.Ergebnis erg = _katalogAufruf.Auswerten(ergebnis);
            _katalogAufruf = null;

            if (erg.Uebernommen != null)
            {
                EmissionsZeile ziel = ZeileZuKuerzel(ErsteZeileMitWert(erg.Uebernommen));
                if (ziel != null) _emissionen.KatalogwertUebernehmen(ziel, erg.Uebernommen);
            }
            if (erg.ArtenGeaendert || erg.WerteGeaendert)
                _emissionen.NeuLadenMitBearbeitungsstand();

            EmissionszeilenSetzen();
            KernwerteSpiegeln();
        }

        /// <summary>Das Kürzel der Art, zu der ein übernommener Katalogwert gehört.</summary>
        private string ErsteZeileMitWert(EmissionswertModel wert)
        {
            if (_emissionen == null || wert == null) return "";
            foreach (EmissionsZeile z in _emissionen.Zeilen)
                if (z.Art != null && z.Art.ID == wert.EmissionsartId) return z.Kuerzel;
            return "";
        }

        private void UnterdialogGeschlossen()
        {
            // Nach jedem Unterdialog wird wie bisher neu gelesen (Risiko R3).
            if (_gewaehlt != null) TraegerWaehlen(_gewaehlt.ID);
        }

        // =====================================================================
        // Bilanzjahr und Unternehmensart (BW4)
        // =====================================================================

        /// <summary>
        /// Das Jahr, für das die Katalogsätze gelesen werden: das Bilanzjahr des
        /// Projekts, ersatzweise <c>BilanzKonvention.BILANZJAHR_RUECKFALL</c>.
        /// Im Katalogkontext gilt das laufende Kalenderjahr. Wortgleich aus
        /// <c>KatalogjahrErmitteln</c> beider Blöcke — zwei verschiedene
        /// Bilanzjahre in einer Maske wären nicht erklärbar.
        /// </summary>
        private static int KatalogjahrErmitteln(int idProjekt, out string unternehmensart,
                                                out double co2PreisProjekt)
        {
            unternehmensart = DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE;
            co2PreisProjekt = 0.0;
            if (idProjekt <= 0) return DateTime.Now.Year;

            try
            {
                WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(idProjekt);
                if (p != null)
                {
                    if (!string.IsNullOrEmpty(p.Unternehmensart)) unternehmensart = p.Unternehmensart;
                    co2PreisProjekt = p.CO2Preis;
                    if (p.BilanzJahr > 0) return p.BilanzJahr;
                }
            }
            catch { }

            return BilanzKonvention.BILANZJAHR_RUECKFALL;
        }

        // =====================================================================
        // Schnellwahlsätze aus dem Gesetzeskatalog (Etappe B4, Befund A7)
        // =====================================================================

        /// <summary>
        /// Rechtfertigt die Unternehmensart den reduzierten Stromsteuersatz?
        /// Produzierendes Gewerbe (§ 2 Nr. 3 StromStG) und Land-/Forstwirtschaft
        /// sind nach § 9b entlastungsberechtigt — dieselbe Bedingung, die
        /// <c>SteuerGutschriftRechner.ProduzierendesGewerbe</c> für die Rechnung
        /// prüft, bis hin zum <c>StringComparison.Ordinal</c>.
        /// </summary>
        private bool ReduzierterSatzEmpfohlen()
        {
            return string.Equals(_unternehmensart, DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                                 StringComparison.Ordinal)
                || string.Equals(_unternehmensart, DbWerte.UNTERNEHMENSART_LAND_FORST,
                                 StringComparison.Ordinal);
        }

        /// <summary>
        /// Ein Stromsteuersatz des Bilanzjahres — wortgleich aus
        /// <c>ucStromAufschlaege.Satz</c>. Anders als beim Brennstoff gibt es
        /// hier IMMER eine Zahl: Zu jedem der beiden Sätze steht eine
        /// Rückfallebene bereit. Die Frage ist nicht „gibt es eine Zahl",
        /// sondern „woher kommt sie" — das sagt die Herkunft.
        /// </summary>
        private Schnellwahlsatz StromsteuerSatz(string schluessel, double rueckfall, bool empfohlen)
        {
            var gesetze = new GesetzKatalog();
            GesetzParameter p = null;
            try { p = gesetze.WertMitHerkunft(schluessel, _katalogJahr); }
            catch { }

            string zweck = string.Equals(schluessel, DbWerte.GESETZ_STROMST_REGELSATZ,
                                         StringComparison.Ordinal)
                ? T("PREIS_ST_ZWECK_REGELFALL", "Stromsteuer im Regelfall (§ 3 StromStG).")
                : T("PREIS_ST_ZWECK_REDUZIERT",
                    "Stromsteuer energieintensiver Unternehmen — was nach der Entlastung "
                    + "nach § 9b StromStG im Preis verbleibt.");

            string herkunft;
            double wert;
            if (p != null && p.Wert.HasValue && InCtKwhStrom(p.Wert.Value, p.Einheit, out wert))
            {
                herkunft = string.Format(T("PREIS_ST_QUELLE", "Katalog: {0} {1} (ab {2}, {3})"),
                    p.Wert.Value.ToString("0.####", CultureInfo.CurrentCulture), p.Einheit,
                    p.JahrVon.ToString(CultureInfo.InvariantCulture), Herkunftstext(p));
            }
            else
            {
                wert = rueckfall;
                herkunft = string.Format(
                    T("PREIS_ST_QUELLE_RUECKFALL",
                      "Rückfallebene: {0} ct/kWh aus dem Programm. {1} "
                      + "Nachpflegbar über „Gesetzliche Parameter\"."),
                    Anzeige(wert),
                    string.Format(T("PREIS_ST_GRUND_KEIN_JAHR",
                        "Der Katalog führt für „{0}\" keinen Satz im Jahr {1}."),
                        schluessel, _katalogJahr.ToString(CultureInfo.InvariantCulture)));
            }

            string voll = zweck + Environment.NewLine + herkunft;
            if (empfohlen)
                voll += Environment.NewLine + string.Format(
                    T("PREIS_ST_EMPFOHLEN",
                      "Vorschlag zur Unternehmensart „{0}\" dieses Projekts. "
                      + "Eingetragen wird der Satz erst mit einem Klick."),
                    UnternehmensartAnzeige(_unternehmensart));

            return new Schnellwahlsatz(Anzeige(wert), voll, wert, empfohlen);
        }

        /// <summary>Für Strom gibt es keine Brennwertbrücke — eine Kilowattstunde
        /// Strom ist eine Kilowattstunde (wie <c>KohaerenzPruefung.Fall4Strom</c>).</summary>
        private static bool InCtKwhStrom(double wert, string einheit, out double ct)
        {
            string e = (einheit ?? "").Trim();
            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_CT_KWH, StringComparison.OrdinalIgnoreCase))
            { ct = wert; return true; }
            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_MWH, StringComparison.OrdinalIgnoreCase))
            { ct = wert / 10.0; return true; }
            ct = 0.0;
            return false;
        }

        private static string UnternehmensartAnzeige(string wert)
        {
            if (string.Equals(wert, DbWerte.UNTERNEHMENSART_PROD_GEWERBE, StringComparison.Ordinal))
                return T("PREIS_ST_ART_PROD_GEWERBE", "produzierendes Gewerbe");
            if (string.Equals(wert, DbWerte.UNTERNEHMENSART_LAND_FORST, StringComparison.Ordinal))
                return T("PREIS_ST_ART_LAND_FORST", "Land- und Forstwirtschaft");
            return T("PREIS_ST_ART_KEIN_PROD_GEWERBE", "kein produzierendes Gewerbe");
        }

        /// <summary>
        /// Ein Energiesteuersatz — wortgleich aus
        /// <c>ucBrennstoffBestandteile.Satz</c>: Katalogschlüssel → Jahressatz →
        /// ct/kWh. Jede Stufe kann leer ausgehen, und dann sagt die Herkunft
        /// welche; eine geratene Zahl gibt es hier nicht (L3).
        /// </summary>
        private Schnellwahlsatz EnergiesteuerSatz(string schluessel, string muster)
        {
            string leer = T("BB_BTN_KEIN_SATZ", "—");

            if (string.IsNullOrEmpty(schluessel))
                return new Schnellwahlsatz(string.Format(muster, leer),
                    T("BB_GRUND_KEIN_SCHLUESSEL",
                      "Diesem Energieträger ist im Katalog kein Energiesteuersatz zugeordnet."),
                    null);

            GesetzParameter p = null;
            try { p = new GesetzKatalog().WertMitHerkunft(schluessel, _katalogJahr); }
            catch { }

            if (p == null || !p.Wert.HasValue)
                return new Schnellwahlsatz(string.Format(muster, leer),
                    string.Format(T("BB_GRUND_KEIN_JAHR",
                        "Der Katalog führt für {0} keinen Satz im Jahr {1}."),
                        schluessel, _katalogJahr), null);

            string grund;
            double? ct = InCtKwhBrennstoff(p.Wert.Value, p.Einheit, out grund);
            if (!ct.HasValue)
                return new Schnellwahlsatz(string.Format(muster, leer), grund, null);

            return new Schnellwahlsatz(
                string.Format(muster, ct.Value.ToString("0.####", CultureInfo.CurrentCulture)),
                string.Format(T("BB_QUELLE", "{0} {1} (ab {2}, {3})"),
                    p.Wert.Value.ToString("0.####", CultureInfo.CurrentCulture),
                    p.Einheit, p.JahrVon, Herkunftstext(p)),
                ct);
        }

        /// <summary>Gramm/Kilowattstunde × Euro/Tonne → Cent/Kilowattstunde.</summary>
        private const double G_KWH_MAL_EUR_T_JE_CT_KWH = 10000.0;

        /// <summary>Gigajoule je Megawattstunde.</summary>
        private const double GJ_JE_MWH = 3.6;

        /// <summary>
        /// Der CO₂-Anteil nach BEHG — wortgleich aus
        /// <c>ucBrennstoffBestandteile.SatzCo2</c>: Preis [€/t] ×
        /// Emissionsfaktor [g/kWh]. Der Preis folgt derselben Vorrangregel wie
        /// der Rechenweg (Projektwert vor Katalogpfad), der Faktor ist das reine
        /// CO₂ und heizwertbezogen.
        /// </summary>
        private Schnellwahlsatz Co2Satz()
        {
            string leer = T("BB_BTN_KEIN_SATZ", "—");
            string muster = T("BB_BTN_CO2", "BEHG: {0}");

            double preis = _co2PreisProjekt;
            string herkunftPreis;
            if (preis > 0.0)
            {
                herkunftPreis = string.Format(T("BB_QUELLE_CO2_PROJEKT", "{0} €/t (Projektwert)"),
                    preis.ToString("0.##", CultureInfo.CurrentCulture));
            }
            else
            {
                GesetzParameter g = null;
                try { g = new GesetzKatalog().WertMitHerkunft(DbWerte.GESETZ_CO2_PREIS_NEHS, _katalogJahr); }
                catch { }
                if (g == null || !g.Wert.HasValue)
                    return new Schnellwahlsatz(string.Format(muster, leer),
                        string.Format(T("BB_GRUND_KEIN_CO2_PREIS",
                            "Der Katalog führt für das Jahr {0} keinen CO₂-Preis."), _katalogJahr),
                        null);
                preis = g.Wert.Value;
                herkunftPreis = string.Format(T("BB_QUELLE_CO2_KATALOG", "{0} €/t (ab {1}, {2})"),
                    preis.ToString("0.##", CultureInfo.CurrentCulture), g.JahrVon, Herkunftstext(g));
            }

            double? ef = null;
            try { ef = EmissionsFaktorLader.Lade(_projektId, _gewaehlt.ID).Co2GKwh; }
            catch { }

            if (!ef.HasValue || ef.Value <= 0.0)
                return new Schnellwahlsatz(string.Format(muster, leer),
                    T("BB_GRUND_KEIN_EF",
                      "Für diesen Energieträger ist kein CO₂-Faktor größer null gepflegt."),
                    null);

            double ct = ef.Value * preis / G_KWH_MAL_EUR_T_JE_CT_KWH;
            return new Schnellwahlsatz(
                string.Format(muster, ct.ToString("0.####", CultureInfo.CurrentCulture)),
                string.Format(T("BB_QUELLE_CO2", "{0} × {1} g/kWh"),
                    herkunftPreis, ef.Value.ToString("0.##", CultureInfo.CurrentCulture)),
                ct);
        }

        /// <summary>
        /// Die Einheitenkette des Konzepts § 6.2 — wortgleich aus
        /// <c>ucBrennstoffBestandteile.InCtKwh</c>. EUR/MWh ist brennwertbezogen
        /// und wird mit Hs/Hi auf den heizwertbezogenen Arbeitspreis gebracht;
        /// fehlt der Brennwert, bleibt der Faktor 1 (konservativ).
        /// </summary>
        private double? InCtKwhBrennstoff(double wert, string einheit, out string grund)
        {
            grund = "";
            string e = (einheit ?? "").Trim();

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_CT_KWH, StringComparison.OrdinalIgnoreCase))
                return wert;

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_MWH, StringComparison.OrdinalIgnoreCase))
            {
                double faktor = (_baseHi > 0.0 && _baseHs > 0.0) ? _baseHs / _baseHi : 1.0;
                return wert / 10.0 * faktor;
            }

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_GJ, StringComparison.OrdinalIgnoreCase))
                return wert * GJ_JE_MWH / 10.0;

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_1000L, StringComparison.OrdinalIgnoreCase))
                return JeTausend(wert, "l", out grund);

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_1000KG, StringComparison.OrdinalIgnoreCase))
                return JeTausend(wert, "kg", out grund);

            grund = string.Format(T("BB_GRUND_EINHEIT_UNBEKANNT",
                "Die Katalogeinheit „{0}\" lässt sich nicht in ct/kWh umrechnen."), e);
            return null;
        }

        /// <summary>
        /// Satz je 1.000 Abrechnungseinheiten → ct/kWh. Die Brücke Liter ↔
        /// Kilogramm bräuchte die Dichte, und <c>energy_carrier.density</c> ist
        /// im gesamten Bestand leer — lieber kein Wert als eine geratene Dichte.
        /// </summary>
        private double? JeTausend(double wert, string erwartet, out string grund)
        {
            grund = "";

            if (!string.Equals(_abrechnungseinheit, erwartet, StringComparison.OrdinalIgnoreCase))
            {
                grund = string.Format(T("BB_GRUND_EINHEIT",
                    "Der Satz gilt je 1.000 {0}; dieser Träger rechnet je {1}. "
                    + "Ohne gepflegte Dichte ist die Umrechnung nicht belegbar."),
                    erwartet, string.IsNullOrEmpty(_abrechnungseinheit) ? "?" : _abrechnungseinheit);
                return null;
            }

            if (_baseHi <= 0.0)
            {
                grund = T("BB_GRUND_HEIZWERT",
                    "Ohne Heizwert lässt sich der Satz nicht in ct/kWh umrechnen.");
                return null;
            }

            return wert / (10.0 * _baseHi);
        }

        private static string Herkunftstext(GesetzParameter p)
        {
            string q = (p.Quelle ?? "").Trim();
            string st = (p.Status ?? "").Trim();
            if (q.Length == 0) return st;
            if (st.Length == 0) return q;
            return q + ", " + st;
        }

        /// <summary>
        /// Die Statuszeilen der beiden Einstiegskacheln — wortgleich aus
        /// <c>Form_Energietraeger.AktualisiereKarten</c>.
        /// </summary>
        private void KartenStatus(EnergietraegerAnsicht a)
        {
            if (a.MitKostenprofil)
            {
                try
                {
                    var vorhandene = new KostenprofilCtrl().ReadAllByProjekt(_projektId);
                    a.KarteProfilStatus = vorhandene.Count == 0
                        ? T("KPROF_STATUS_KEIN_PROFIL", "Noch kein Profil hinterlegt.")
                        : vorhandene[0].Bezeichner;
                }
                catch { a.KarteProfilStatus = "—"; }
            }

            try
            {
                var reihen = new PreisreiheCtrl().ReadVerfuegbare(_projektId);
                if (reihen.Count == 0)
                    a.KarteSpotStatus = T("KDLG_ET_SPOT_KEINE", "Noch keine Preisreihe vorhanden.");
                else
                {
                    int min = int.MaxValue, max = int.MinValue;
                    foreach (PreisreiheModel m in reihen)
                    {
                        if (m.Jahr < min) min = m.Jahr;
                        if (m.Jahr > max) max = m.Jahr;
                    }
                    a.KarteSpotStatus = string.Format(CultureInfo.CurrentCulture,
                        T("KDLG_ET_SPOT_STATUS", "{0} Reihe(n), Jahre {1}–{2}"),
                        reihen.Count, min, max);
                }
            }
            catch { a.KarteSpotStatus = "—"; }
        }

        /// <summary>Statuszeile der Saisonreihe (FK6a) — wortgleich aus
        /// <c>ucFuelSettings.ZeigeReihenStatus</c>.</summary>
        private string ReihenStatus()
        {
            if (_gewaehlt == null) return "";
            try
            {
                PreisreiheModel r = new PreisreiheCtrl().ReadTraegerReihe(_projektId, _gewaehlt.ID);
                return r == null ? "" : string.Format(CultureInfo.CurrentCulture,
                    T("KDLG_LP_REIHE_STATUS", "Saisonreihe {0} ({1}) — gilt vor dem Satz."),
                    r.Jahr,
                    r.IstStamm ? T("KDLG_LPR_EBENE_STAMM", "Stammreihe (Katalog)")
                               : T("KDLG_LPR_EBENE_PROJEKT", "Projektreihe"));
            }
            catch { return ""; }
        }

        // =====================================================================
        // Texte der Trägerkarte und der beiden Preisblöcke
        // =====================================================================

        /// <summary>
        /// Die Texte der Trägerkarte. Sie stehen als Satz, weil die Komponente
        /// sie an einen verschachtelten Baustein weiterreicht — einzeln
        /// durchgereicht wären es dreißig Parameter auf zwei Ebenen.
        /// </summary>
        private static IReadOnlyDictionary<string, object> KarteTexte()
        {
            return new Dictionary<string, object>
            {
                ["TitelPreise"] = T("KDLG_ET_TAB_PREISE", "Preise & Umrechnung"),
                ["TitelUmrechnung"] = MyResource.Resource.KOSTEN_UMRECHNUNG_TITEL,
                ["TitelEmissionen"] = T("KDLG_ET_TAB_EMISSIONEN", "Emissionen"),
                ["TitelHistorie"] = T("ETV_TITEL_HISTORIE", "Preishistorie"),
                ["LabelPreisbasis"] = T("ETV_LBL_PREISBASIS", "Preisbasis"),
                ["LabelBasiseinheit"] = T("ETV_LBL_BASISEINHEIT", "Basiseinheit:"),
                ["LabelArbeitspreis"] = T("ETV_LBL_ARBEITSPREIS", "Arbeitspreis"),
                ["LabelLeistungspreis"] = T("ETV_LBL_LEISTUNGSPREIS", "Leistungspreis"),
                ["LabelGrundpreis"] = T("ETV_LBL_GRUNDPREIS", "Grundpreis"),
                ["LabelHeizwert"] = T("ETV_LBL_HEIZWERT", "Heizwert"),
                ["LabelBrennwert"] = T("ETV_LBL_BRENNWERT", "Brennwert"),
                ["LabelPreisJeKwh"] = T("ETV_LBL_PREIS_JE_KWH", "Preis pro kWh:"),
                ["LabelFormel"] = T("ETV_LBL_FORMEL", "Formel:"),
                ["ModusJahrText"] = T("KDLG_LP_MODUS_JAHR", "Jahresleistungspreis"),
                ["ModusMonatText"] = T("KDLG_LP_MODUS_MONAT", "Monatsleistungspreis"),
                ["SaisonText"] = T("KDLG_LP_SAISON", "Saisonale Sätze…"),
                ["SpalteName"] = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_NAME,
                ["SpalteVon"] = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_VON,
                ["SpalteNach"] = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_NACH,
                ["SpalteFaktor"] = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_FAKTOR,
                ["SpalteAktiv"] = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_AKTIV,
                ["RegelNeuText"] = MyResource.Resource.KOSTEN_UMRECHNUNG_NEU,
                ["AufschlagAnwendenText"] = T("KDLG_AUFSCHLAG_ANWENDEN",
                    "Aufschläge in der Wirtschaftlichkeit berücksichtigen"),
                ["LabelModus"] = T("KDLG_EM_MODUS", "CO₂-Berechnung:"),
                ["ModusCo2Text"] = T("KDLG_EM_MODUS_CO2", "CO₂"),
                ["ModusCo2eText"] = T("KDLG_EM_MODUS_CO2E", "CO₂-Äquivalent (GWP₁₀₀)"),
                ["SpalteArt"] = T("KDLG_EM_SP_ART", "Art"),
                ["SpalteWert"] = T("KDLG_EM_SP_WERT", "Wert"),
                ["SpalteEinheit"] = T("KDLG_EM_SP_EINHEIT", "Einheit"),
                ["SpalteHerkunft"] = T("KDLG_EM_SP_HERKUNFT", "Herkunft"),
                ["KatalogZeileText"] = T("KDLG_EM_KATALOG", "Katalog…"),
                ["KatalogVerwaltenText"] = T("KDLG_EM_VERWALTEN",
                    "Emissionsarten & Katalog verwalten…"),
                ["KeinKatalogText"] = T("KDLG_EM_KEIN_KATALOG",
                    "Der Emissionsarten-Katalog ist auf dieser Datenbank nicht verfügbar "
                    + "(Migrationsschritt 57 fehlt). Es gelten die drei Bestandsfelder."),
                ["LabelCo2"] = T("ETV_LBL_CO2", "CO2  [g/kWh]"),
                ["LabelSo2"] = T("ETV_LBL_SO2", "SO2  [g/kWh]"),
                ["LabelNox"] = T("ETV_LBL_NOX", "NOx  [g/kWh]"),
                ["LabelGueltigAb"] = T("ETV_LBL_GUELTIG_AB", "Gültig ab"),
                ["SpeichernText"] = T("ETV_BTN_SPEICHERN", "💾 Speichern"),
                ["SpalteGueltigAb"] = T("ETV_SP_GUELTIG_AB", "Gültig ab"),
                ["SpalteHeizwert"] = T("ETV_SP_HEIZWERT", "Heizwert"),
                ["SpalteBasisEinheit"] = T("ETV_SP_BASISEINHEIT", "Basis Einheit"),
                ["SpalteArbeitspreis"] = T("ETV_SP_ARBEITSPREIS", "Arbeitspreis"),
                ["SpalteGrundpreis"] = T("ETV_SP_GRUNDPREIS", "Grundpreis [€/a]"),
                ["SpalteLeistungspreis"] = T("ETV_SP_LEISTUNGSPREIS", "Leistungspreis")
            };
        }

        /// <summary>Die Texte des Aufschlagsblocks — wortgleich aus
        /// <c>ucStromAufschlaege.TexteSetzen</c>.</summary>
        private static IReadOnlyDictionary<string, object> AufschlagTexte()
        {
            return new Dictionary<string, object>
            {
                ["TitelAufschlag"] = MyResource.Resource.PREIS_GRUPPE_AUFSCHLAG,
                ["TitelVerguetung"] = MyResource.Resource.PREIS_GRUPPE_VERGUETUNG,
                ["ModusAufgeschluesselt"] = MyResource.Resource.PREIS_MODUS_AUFGESCHLUESSELT,
                ["ModusGesamtwert"] = MyResource.Resource.PREIS_MODUS_GESAMTWERT,
                ["LabelNetzentgelt"] = MyResource.Resource.PREIS_KOMP_NETZENTGELT,
                ["LabelUmlagen"] = MyResource.Resource.PREIS_KOMP_UMLAGEN,
                ["LabelStromsteuer"] = MyResource.Resource.PREIS_KOMP_STROMSTEUER,
                ["LabelKonzession"] = MyResource.Resource.PREIS_KOMP_KONZESSION,
                ["LabelVertrieb"] = MyResource.Resource.PREIS_KOMP_VERTRIEB,
                ["LabelGesamtaufschlag"] = MyResource.Resource.PREIS_LABEL_GESAMTAUFSCHLAG,
                ["LabelVerguetungPv"] = MyResource.Resource.PREIS_LABEL_VERGUETUNG_PV,
                ["LabelVerguetungBhkw"] = MyResource.Resource.PREIS_LABEL_VERGUETUNG_BHKW,
                ["Einheit"] = DbWerte.PREISREIHE_EINHEIT_CT_KWH
            };
        }

        /// <summary>Die Texte der Preiszerlegung — wortgleich aus
        /// <c>ucBrennstoffBestandteile.TexteSetzen</c>.</summary>
        private static IReadOnlyDictionary<string, object> BestandteilTexte()
        {
            return new Dictionary<string, object>
            {
                ["TitelBestandteile"] = T("BB_GRUPPE_BESTANDTEILE", "Preisbestandteile des Brennstoffs"),
                ["ModusAufgeschluesselt"] = T("BB_MODUS_AUFGESCHLUESSELT",
                    "aufgeschlüsselt (Summe ist der Preis)"),
                ["ModusGesamtwert"] = T("BB_MODUS_GESAMTWERT", "Gesamtwert (Arbeitspreis gilt)"),
                ["LabelSchnellwahl"] = T("BB_SCHNELLWAHL", "Schnellwahl (Katalog):"),
                ["LabelEnergiesteuer"] = T("BB_KOMP_ENERGIESTEUER", "Energiesteuer"),
                ["LabelCo2"] = T("BB_KOMP_CO2", "CO₂-Anteil (BEHG)"),
                ["LabelNetzentgelt"] = T("BB_KOMP_NETZENTGELT", "Netz-/Messentgelt"),
                ["LabelVertrieb"] = T("BB_KOMP_VERTRIEB", "Vertrieb"),
                ["LabelArbeitspreis"] = T("BB_LABEL_ARBEITSPREIS", "Arbeitspreis (Trägerdialog)"),
                ["InArbeitspreisText"] = T("BB_BTN_IN_ARBEITSPREIS", "In Arbeitspreis übernehmen"),
                ["Einheit"] = DbWerte.PREISREIHE_EINHEIT_CT_KWH
            };
        }

        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }
    }
}
