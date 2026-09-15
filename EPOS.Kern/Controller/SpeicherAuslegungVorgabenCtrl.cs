using System;
using System.Collections.Generic;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// DER GESPEICHERTE STAND der Ansicht "Auslegung optimieren" — die Eingaben, so
    /// wie sie der Anwender zuletzt gesetzt hat.
    /// </summary>
    /// <remarks>
    /// <para><b>Ein VERAENDERLICHER Typ mit Absicht.</b> Eine Ansicht fuehrt die
    /// halbfertige Eingabe: "Kapazitaet von" ist leer, waehrend der Anwender "bis"
    /// tippt. Dieser Typ haelt genau diesen Zwischenzustand; geprueft wird erst, wenn
    /// gerechnet werden soll (<c>StromspeicherAuslegungCtrl.Vorpruefen</c>).</para>
    /// <para><b>Er wird als JSON aufbewahrt</b> (<c>Tab_SpeicherAuslegung.Daten</c>,
    /// <see cref="SpeicherAuslegungCtrl.Serialisieren"/>). Ein aelterer Stand kann
    /// deshalb Felder tragen, die es hier nicht mehr gibt — der Leser ueberliest sie,
    /// er scheitert nicht daran. Der Suchbereich der Flotte steht seit dem Wegfall des
    /// Einzelspeicher-Rasters je Einheit in <c>FlottenAuslegungsAchse</c> und nicht
    /// mehr hier.</para>
    /// </remarks>
    public sealed class SpeicherOptimierungEingaben
    {
        /// <summary>Quellen, Kosten und Profile der Auslegung.</summary>
        public SpeicherAuslegungKonfiguration Auslegung { get; set; }

        /// <summary>
        /// Leistungspreis L_P [EUR/(kW*a)] des Betriebsziels Lastspitzenkappung
        /// (Anwenderentscheid W11b-E-3, 10.09.2026).
        /// </summary>
        /// <remarks>
        /// Vorbelegt aus <c>Tab_StromspeicherVariante.L_P</c> der aktiven Variante —
        /// dasselbe Feld, das der Reiter "Parameter" und die Peak-Shaving-Maske pflegen.
        /// Die Ansicht schreibt eine Aenderung SOFORT dorthin zurueck; es gibt genau eine
        /// Pflegestelle, nicht drei nebeneinanderher laufende Werte.
        /// </remarks>
        public double LeistungspreisEurProKwA { get; set; }

        /// <summary>Eine unabhaengige Kopie — die Ansicht schreibt in ihre eigene.</summary>
        public SpeicherOptimierungEingaben Kopie()
        {
            return new SpeicherOptimierungEingaben
            {
                Auslegung = SpeicherAuslegungKopie.Von(Auslegung),
                LeistungspreisEurProKwA = LeistungspreisEurProKwA
            };
        }
    }

    /// <summary>
    /// Eine QUELLE, aus der der Leistungspreis in die Maske übernommen werden kann
    /// (Anwenderentscheid W11b‑E‑3, 10.09.2026).
    /// </summary>
    /// <remarks>
    /// Der Anwender hat beides verlangt: „Leistungspreis in Maske und alternativ aus
    /// Leistungspreis Tarifstruktur/Energieträger Strom (Übernahme in die Maske als
    /// Auswahl)". Die Übernahme ist deshalb ein ANGEBOT und keine automatische
    /// Vorbelegung — die drei Werte sind fachlich verschieden (die Variante trägt den
    /// für den Speicher gültigen, die Tarifstruktur den der Wirtschaftlichkeitsrechnung,
    /// der Energieträger den des Kostenmoduls), und welcher gilt, entscheidet der
    /// Anwender.
    /// </remarks>
    public sealed class SpeicherOptimierungLeistungspreisQuelle
    {
        /// <summary>Der Anzeigetext: Herkunft, Wert und die Begründung der Staffelstufe.</summary>
        public string Bezeichnung { get; set; } = "";

        /// <summary>Der Leistungspreis dieser Quelle [€/(kW·a)].</summary>
        public double WertEurProKwA { get; set; }
    }

    /// <summary>Vorbelegung des Suchraums samt der aktuellen Auslegung.</summary>
    public sealed class SpeicherOptimierungVorgaben
    {
        public IReadOnlyList<SpeicherAuslegungProfil> Auslegungsprofile { get; set; } = Array.Empty<SpeicherAuslegungProfil>();
        public IReadOnlyList<KostenprofilModel> Strompreisprofile { get; set; } = Array.Empty<KostenprofilModel>();
        public SpeicherKostensaetze Modulkosten { get; set; } = new();
        /// <summary>Der vorgeschlagene Suchraum.</summary>
        public SpeicherOptimierungEingaben Eingaben { get; set; } = new SpeicherOptimierungEingaben();

        /// <summary>„Aktuelle Auslegung: … kWh / … kW (… C)"; leer, wenn keine da ist.</summary>
        public string AktuelleAuslegung { get; set; } = "";

        /// <summary>
        /// Die verfügbaren Leistungspreis-Quellen (W11b‑E‑3). Leer heißt „keine
        /// gepflegt" — dann bietet der Dialog keine Übernahme an, statt eine Liste mit
        /// Nullen zu zeigen.
        /// </summary>
        public IReadOnlyList<SpeicherOptimierungLeistungspreisQuelle> Leistungspreisquellen { get; set; }
            = new List<SpeicherOptimierungLeistungspreisQuelle>();
    }

    /// <summary>
    /// Die VORBELEGUNG der Stromspeicher-Auslegung: der gespeicherte Stand der Ansicht
    /// "Auslegung optimieren" und die Quellen, aus denen der Leistungspreis in sie
    /// uebernommen werden kann.
    /// </summary>
    /// <remarks>
    /// <para><b>Was hier steht und was nicht.</b> Diese Klasse liest die Datenbank und
    /// baut daraus einen Vorschlag - mehr nicht. Gerechnet wird an anderer Stelle: Die
    /// Ansicht kennt EINEN Weg, die Flottenrechnung
    /// (<see cref="SpeicherFlottenStudieCtrl"/>, <c>FlottenOptimierer</c>), und
    /// ein Einzelspeicher ist dort eine Flotte mit einer Einheit.</para>
    /// <para><b>Der einzige Aufrufer der Vorbelegung ist
    /// <see cref="SpeicherAuslegungCtrl.Vorbelegung(int, double)"/></b>, die den
    /// Vorschlag um Modulkosten, Strompreisprofile und den gespeicherten Stand
    /// ergaenzt. Die Trennung bleibt, weil hier der Teil steht, der ohne
    /// Auslegungsprofile auskommt.</para>
    /// <para><b>Datenbankzugriff - gehoert auf den Bedienfaden.</b> Jeder Lesefehler ist
    /// stumm und endet in einem leeren Angebot: Ein fehlender Tarifsatz darf die
    /// Ansicht nicht verhindern.</para>
    /// </remarks>
    public static class SpeicherAuslegungVorgabenCtrl
    {
        /// <summary>
        /// Liest die aktuelle Auslegung und die Leistungspreis-Quellen
        /// (<b>Datenbankzugriff</b> — gehört auf den Bedienfaden).
        /// </summary>
        /// <remarks>
        /// Den Suchbereich schlägt diese Methode nicht mehr vor: Er steht je Einheit in
        /// der Flottenachse (<c>FlottenAuslegungsAchse</c>). Was hier entsteht, ist die
        /// Zeile „Aktuelle Auslegung: … kWh / … kW (… C)" und das Angebot an
        /// Leistungspreisen.
        /// </remarks>
        public static SpeicherOptimierungVorgaben Vorbelegung(int idProjekt)
        {
            return Vorbelegung(idProjekt, 0.0);
        }

        /// <summary>
        /// Wie <see cref="Vorbelegung(int)"/>, dazu die BEZUGSSPITZE des Lastgangs
        /// [kW] — sie entscheidet, welche Stufe der Leistungspreis-Staffel an der Spitze
        /// greift (Anwenderentscheid W11b‑E‑3, 10.09.2026).
        /// </summary>
        /// <remarks>
        /// Die Spitze kommt aus dem Lauf und nicht aus der Datenbank; ohne gelaufene
        /// Simulation ist sie 0, und die Staffel wird dann mit der unteren Stufe
        /// angeboten — mit ausgewiesener Begründung, damit der angebotene Wert nicht
        /// falsch verstanden wird.
        /// </remarks>
        /// <param name="idProjekt">Das Projekt.</param>
        /// <param name="bezugsspitzeKw">Höchste Bezugsleistung des Lastgangs [kW]; 0 = unbekannt.</param>
        public static SpeicherOptimierungVorgaben Vorbelegung(int idProjekt, double bezugsspitzeKw)
        {
            SpeicherOptimierungVorgaben vorgaben = new SpeicherOptimierungVorgaben();
            CultureInfo k = CultureInfo.CurrentCulture;

            double cNom = 0.0, pKw = 0.0;
            try
            {
                SpeicherParameter aktuell = new StromspeicherSimCtrl().LeseParameter(idProjekt);
                if (aktuell != null)
                {
                    cNom = aktuell.CNomKwh;
                    pKw = aktuell.PKw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die aktuelle Speicherauslegung konnte nicht gelesen werden: " + ex.Message);
            }

            if (cNom > 0.0)
            {
                vorgaben.AktuelleAuslegung = string.Format(k, MyResource.Resource.OPT_LBL_AKTUELL,
                    cNom.ToString("0.#", k), pKw.ToString("0.#", k),
                    (pKw / cNom).ToString("0.##", k));
            }

            // Der Leistungspreis kommt aus der AKTIVEN VARIANTE — dieselbe Zeile, die der
            // Reiter „Parameter" und die Peak-Shaving-Maske pflegen (W11b‑E‑3).
            try
            {
                StromspeicherVarianteModel variante =
                    new StromspeicherVarianteCtrl().ReadAktiveVariante(idProjekt);
                if (variante != null && variante.L_P > 0.0)
                    vorgaben.Eingaben.LeistungspreisEurProKwA = variante.L_P;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Der Leistungspreis der Variante konnte nicht gelesen werden: " + ex.Message);
            }

            vorgaben.Leistungspreisquellen = Leistungspreisquellen(idProjekt, bezugsspitzeKw);
            return vorgaben;
        }

        /// <summary>
        /// Die verfügbaren ALTERNATIVQUELLEN des Leistungspreises, jede mit Wert und
        /// erklärender Beschriftung (Anwenderentscheid W11b‑E‑3, 10.09.2026).
        /// </summary>
        /// <remarks>
        /// <para><b>(1) Tarifstruktur der Wirtschaftlichkeit</b> —
        /// <c>Tab_ProjektTarif</c> führt eine zweistufige Staffel je Stammprojekt:
        /// bis <c>Staffel_Grenze</c> gilt <c>Staffel_Preis1</c>, darüber
        /// <c>Staffel_Preis2</c> [€/(kW·a)]. Angeboten wird der Preis der Stufe, in der
        /// die BEZUGSSPITZE liegt, und zwar aus einem fachlichen Grund: Eine Kappung
        /// nimmt die Leistung IMMER von oben weg. Die erste eingesparte Kilowattstunde
        /// Leistung ist deshalb die teuerste — die der obersten Stufe. Reicht die Kappung
        /// unter die Staffelgrenze, ist der so bewertete Ertrag zu hoch; das nennt der
        /// Text, und der Anwender kann den Wert von Hand ändern.</para>
        ///
        /// <para><b>(2) Energieträger Strom</b> — der Leistungspreis des im Projekt
        /// verwendeten Stromträgers, vorrangig die Projektübersteuerung
        /// (<c>energy_project_settings.custom_price_power</c>), sonst der jüngste
        /// Katalogstand (<c>energy_price.leistungspreis</c>). <b>Die Einheit hängt am
        /// Modus des Trägers</b> (KD4/FK6): JAHR ist €/(kW·a) und geht unverändert ein,
        /// MONAT ist €/(kW·Monat) und wird mit zwölf multipliziert — dieselbe Umrechnung
        /// wie in <c>KostenEmissionRechner</c>. Ohne diese Wache stünde in der Maske ein
        /// Zwölftel des richtigen Wertes.</para>
        ///
        /// <para><b>Nur lesend, und jeder Fehler ist stumm.</b> Was diese Methode kann,
        /// ist ein ANGEBOT machen; eine fehlende Tabelle darf den Dialog nicht
        /// verhindern.</para>
        /// </remarks>
        public static IReadOnlyList<SpeicherOptimierungLeistungspreisQuelle> Leistungspreisquellen(
            int idProjekt, double bezugsspitzeKw)
        {
            var quellen = new List<SpeicherOptimierungLeistungspreisQuelle>();
            if (idProjekt <= 0) return quellen;

            CultureInfo k = CultureInfo.CurrentCulture;

            try
            {
                // Der Tarif hängt am STAMM der Vergleichsgruppe, nicht an der Variante.
                int idStamm = new VariantenCtrl().StammRefDerVariante(idProjekt);
                if (idStamm <= 0) idStamm = idProjekt;

                SpeicherOptimierungLeistungspreisQuelle ausTarif =
                    TarifQuelle(new WirtschaftlichkeitCtrl().LadeTarif(idStamm), bezugsspitzeKw);
                if (ausTarif != null) quellen.Add(ausTarif);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Tarifstruktur konnte nicht gelesen werden: " + ex.Message);
            }

            try
            {
                int idTraeger = Emissionsquelle.StromTraeger(idProjekt);
                if (idTraeger > 0)
                {
                    double? preis = null;

                    EnergietraegerPreisCtrl.Projektpreis projekt =
                        EnergietraegerPreisCtrl.ProjektpreisLesen(idProjekt, idTraeger);
                    if (projekt != null && projekt.Leistungspreis.HasValue && projekt.Leistungspreis.Value > 0.0)
                        preis = projekt.Leistungspreis.Value;

                    if (!preis.HasValue)
                    {
                        // Historie kommt jüngste zuerst — der erste gepflegte Wert gilt.
                        foreach (EnergietraegerPreisCtrl.Historienzeile zeile in
                                 EnergietraegerPreisCtrl.Historie(idTraeger, null))
                            if (zeile.Leistungspreis.HasValue && zeile.Leistungspreis.Value > 0.0)
                            {
                                preis = zeile.Leistungspreis.Value;
                                break;
                            }
                    }

                    if (preis.HasValue)
                    {
                        double wert = preis.Value;
                        if (string.Equals(EnergietraegerPreisCtrl.LeistungsModus(idTraeger),
                                          DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal))
                            wert *= 12.0;

                        quellen.Add(new SpeicherOptimierungLeistungspreisQuelle
                        {
                            WertEurProKwA = wert,
                            Bezeichnung = string.Format(k, MyResource.Resource.OPT_QUELLE_ENERGIETRAEGER,
                                wert.ToString("0.##", k))
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Der Leistungspreis des Stromträgers konnte nicht gelesen werden: " + ex.Message);
            }

            return quellen;
        }

        /// <summary>
        /// Die Leistungspreis-Quelle „Tarifstruktur" aus einem Tarifsatz;
        /// <c>null</c>, wenn keine Staffel gepflegt ist (W11b‑E‑3, 10.09.2026).
        /// </summary>
        /// <remarks>
        /// <b>Ohne Datenbank prüfbar</b> — deshalb steht die Stufenwahl hier und nicht
        /// mitten im Leseweg. Die Regel: Eine Kappung nimmt die Leistung IMMER von oben
        /// weg, die erste eingesparte Kilowatt ist also die der obersten Stufe. Liegt
        /// die Bezugsspitze über der Staffelgrenze und ist ein Preis der zweiten Stufe
        /// gepflegt, gilt dieser; sonst der erste. Ist die Spitze unbekannt (kein
        /// gelaufener Durchgang), wird die untere Stufe angeboten und der Text sagt es.
        /// </remarks>
        /// <param name="tarif">Der Tarifsatz des Stammprojekts; <c>null</c> ist zulässig.</param>
        /// <param name="bezugsspitzeKw">Höchste Bezugsleistung [kW]; 0 = unbekannt.</param>
        public static SpeicherOptimierungLeistungspreisQuelle TarifQuelle(
            TarifParameter tarif, double bezugsspitzeKw)
        {
            if (tarif == null) return null;

            CultureInfo k = CultureInfo.CurrentCulture;
            double grenze = Math.Max(0.0, tarif.StaffelGrenzeKW);
            bool stufe2 = bezugsspitzeKw > grenze && tarif.StaffelPreis2EurKW > 0.0;
            double wert = stufe2 ? tarif.StaffelPreis2EurKW : tarif.StaffelPreis1EurKW;
            if (!(wert > 0.0)) return null;

            string grund = bezugsspitzeKw <= 0.0
                ? MyResource.Resource.OPT_QUELLE_TARIF_OHNE_SPITZE
                : string.Format(k,
                    stufe2 ? MyResource.Resource.OPT_QUELLE_TARIF_STUFE2
                           : MyResource.Resource.OPT_QUELLE_TARIF_STUFE1,
                    bezugsspitzeKw.ToString("0.#", k), grenze.ToString("0.#", k));

            return new SpeicherOptimierungLeistungspreisQuelle
            {
                WertEurProKwA = wert,
                Bezeichnung = string.Format(k, MyResource.Resource.OPT_QUELLE_TARIF,
                    wert.ToString("0.##", k), grund)
            };
        }
    }
}
