using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Ablauf des Gebäudeexports</b> (Stufe G7a; Softwarearchitektur 1.6, Datenaustauschkonzept
    /// Kapitel 5) — das Spiegelbild des Imports: <see cref="Vorbereiten"/> bildet aus dem fertig gelesenen
    /// <see cref="GebaeudeExportSatz"/> das formatfreie <see cref="GebaeudeAbbild"/> samt allen Meldungen
    /// und berührt die Datenbank nicht; <see cref="Schreiben"/> gibt es an den Schreiber des Profils.
    ///
    /// <para><b>Wirksame Werte über dieselben Funktionen wie der Lauf:</b> jede Zeile über
    /// <see cref="GebaeudeZonenabbildung.AlsBauteil"/> (Art, Randbedingung nach
    /// <see cref="GebaeudeZonenabbildung.LeerHeisstInnen"/>, wirksame Neigung, Schichten), die
    /// Übergangswiderstände über <see cref="Bauteilreduktion.Uebergangswiderstaende"/>, die
    /// Gruppenkapazitäten über <see cref="ErsatzparameterRC.GruppenkapazitaetenAusBauteilweg"/>; ein leerer
    /// Zonenwert ist der des Gebäudes, die inneren Gewinne der Zone ihr Anteil nach dem Flächenschlüssel.</para>
    ///
    /// <list type="bullet">
    /// <item><b>Bauteile</b> nach der Umkehrtabelle (<see cref="GbxmlUmkehrung"/>): Flächenart, Nachbarn
    /// (eigener Raum zuerst, mit seiner Sicht), Platzhalter „unbeheizt" je Gebäude; ein Wechsel wird
    /// gemeldet, eine Ablehnung beendet den Export. Eine Trennfläche zur Nachbarzone ist eine
    /// Innenfläche mit dem Raum der Nachbarzone.</item>
    /// <item><b>Öffnungen</b> im Wirt: dieselbe Zone, dieselbe Randbedingung, gleicher Azimut und gleiche
    /// wirksame Neigung (auf 1e-6); sonst der erste Wirt gleicher Randbedingung und Neigungsklasse, mit
    /// Meldung; sonst Ablehnung. Der Wirt wird brutto geschrieben.</item>
    /// <item><b>Innenzeilen</b> (E45): Paare gleicher Art, gleicher Fläche und gleichen oder gespiegelten
    /// Aufbaus werden EINE Fläche mit zweimal demselben Raum; eine Zeile ohne Partner halb, beidseitig,
    /// mit Meldung.</item>
    /// <item><b>Aufbauten</b> je Übergangsfall (Richtung, Randbedingung) mit dem U-Wert dieses Falls; ruhende
    /// Luftschicht als Dicke und Widerstand der Tabelle 8 in der Richtung des Bauteils (gemessen: sie kehrt
    /// über den Namensabgleich als Luftschicht zurück); eine Luftschicht mit äquivalentem λ unter 5 kg/m³
    /// auf 5 kg/m³ angehoben, mit Meldung.</item>
    /// <item><b>Ersatzschichtung</b> (D10, Datenaustauschkonzept 5.3) für ein Bauteil ohne Schichten: EINE
    /// Schicht mit U und der flächenbezogenen Kapazität, mit der die Gruppe im Lauf rechnet, in den
    /// Stoffwertbändern; sonst masselos mit Meldung. Der Vorbehalt steht wörtlich in
    /// <c>Construction/Description</c>, im Namen des Stoffs und in der Meldung.</item>
    /// <item><b>Räume und Zonen</b>: Fläche, Volumen, Infiltration, Personen, innere Gewinne je Fläche,
    /// Heizsollwert; gekühlt nur mit Projektschalter UND Kühlschalter der Zone.</item>
    /// <item><b>Kopf</b>: Gebäudeart nach der Tabelle des Profils, Produktausweis in
    /// <c>Campus/Description</c> und je Zone, Wasserzeichen der Testlizenz, Ort nur mit PLZ.</item>
    /// </list>
    /// </summary>
    internal sealed class GebaeudeExportAblauf
    {
        // ==================================================================
        //  Meldungskennungen (Texte: GEXP_PROT_* der Ressourcen)
        // ==================================================================

        /// <summary>Präfix der Meldungsschlüssel.</summary>
        internal const string P = "GEXP_PROT_";

        /// <summary>F — Das Projektgebäude hat keine Projektkopie.</summary>
        internal const string KEIN_GEBAEUDE = P + "KEIN_GEBAEUDE";
        /// <summary>F — {0} Grund: Der Übernahmevorschlag des Klassenwegs ließ sich nicht bilden.</summary>
        internal const string UEBERNAHME_ABGELEHNT = P + "UEBERNAHME_ABGELEHNT";
        /// <summary>I — {0} Eintrag: ein Eintrag der Hochrechnung aus dem Simulationsprotokoll.</summary>
        internal const string UEBERNAHME_PROTOKOLL = P + "UEBERNAHME_PROTOKOLL";
        /// <summary>I — {0} Laufzeit [ms] der Hochrechnung auf dem lesenden Faden.</summary>
        internal const string UEBERNAHME_LAUFZEIT = P + "UEBERNAHME_LAUFZEIT";
        /// <summary>I — {0} Faktor, {1} Grundlage: Klassenweg, Hülle hochgerechnet.</summary>
        internal const string KLASSENWEG = P + "KLASSENWEG";
        /// <summary>F — {0} Zahl der Flächen, {1} Mindestzahl.</summary>
        internal const string ZU_WENIG_FLAECHEN = P + "ZU_WENIG_FLAECHEN";
        /// <summary>F — {0} Bauteil, {1} Grund: Die Zeile lässt sich nicht abbilden (wie im Lauf).</summary>
        internal const string BAUTEIL_UNGUELTIG = P + "BAUTEIL_UNGUELTIG";
        /// <summary>F — {0} Bauteil: Die Zeile ist noch nicht gespeichert (keine Kennung).</summary>
        internal const string BAUTEIL_UNGESPEICHERT = P + "BAUTEIL_UNGESPEICHERT";
        /// <summary>F — {0} Bauteil, {1} Grund der Umkehrtabelle.</summary>
        internal const string UMKEHR_ABLEHNUNG = P + "UMKEHR_ABLEHNUNG";
        /// <summary>F — {0} Bauteil: Die Nachbarzone gehört nicht zum Gebäude.</summary>
        internal const string NACHBARZONE_UNBEKANNT = P + "NACHBARZONE_UNBEKANNT";
        /// <summary>F — {0} Öffnung: kein Wirt gleicher Randbedingung und Neigungsklasse.</summary>
        internal const string OEFFNUNG_OHNE_WIRT = P + "OEFFNUNG_OHNE_WIRT";
        /// <summary>W — {0} Öffnung, {1} Wirt: Ersatzwirt; Azimut und Neigung der Öffnung gehen verloren.</summary>
        internal const string OEFFNUNG_ERSATZWIRT = P + "OEFFNUNG_ERSATZWIRT";
        /// <summary>I — {0} Bauteil, {1} Grund: benannter Wechsel der Umkehrtabelle.</summary>
        internal const string WECHSEL = P + "WECHSEL";
        /// <summary>W — {0} Bauteil, {1} U: Der eingetragene U-Wert neben dem Aufbau geht verloren.</summary>
        internal const string U_NEBEN_AUFBAU = P + "U_NEBEN_AUFBAU";
        /// <summary>I — {0} Zahl der Bauteile, {1} Vorbehalt: Ersatzschichtung.</summary>
        internal const string ERSATZSCHICHTUNG = P + "ERSATZSCHICHTUNG";
        /// <summary>W — {0} Bauteil, {1} Grund: masselos geschrieben (nur Widerstand).</summary>
        internal const string MASSELOS = P + "MASSELOS";
        /// <summary>I — {0} Zone, {1} Innenfläche A_IW [m²]: Ersatzfläche innerer Masse.</summary>
        internal const string INNENMASSE = P + "INNENMASSE";
        /// <summary>I — {0} Bauteil: Innenzeile ohne Partner, halb und beidseitig geschrieben.</summary>
        internal const string INNEN_HALBZEILE = P + "INNEN_HALBZEILE";
        /// <summary>I — {0} Aufbau, {1} Reihenfolge: Rohdichte bzw. Wärmekapazität einer Luftschicht gesetzt.</summary>
        internal const string LUFTSCHICHT_ANGEHOBEN = P + "LUFTSCHICHT_ANGEHOBEN";
        /// <summary>I — {0} Gebäudeart: ohne Treffer der Tabelle, <c>Unknown</c>.</summary>
        internal const string GEBAEUDEART_UNBEKANNT = P + "GEBAEUDEART_UNBEKANNT";
        /// <summary>I — Ohne PLZ: kein Standort, keine Nordrichtung.</summary>
        internal const string OHNE_ORT = P + "OHNE_ORT";
        /// <summary>I — Testlizenz: Wasserzeichen in der Datei.</summary>
        internal const string TESTLIZENZ = P + "TESTLIZENZ";
        /// <summary>I — {0} Felder: die benannten Verluste, die das Gebäude trägt.</summary>
        internal const string VERLUSTE = P + "VERLUSTE";

        /// <summary>Grund „masselos": Die Gruppe rechnet mit Schichten; ein Bauteil ohne Schichten trägt dort keine Kapazität.</summary>
        internal const string GRUND_GEMISCHT = "GEMISCHTE_GRUPPE";
        /// <summary>Grund „masselos": Die Stoffwertbänder haben für U und Kapazität keinen gemeinsamen Schnitt.</summary>
        internal const string GRUND_SCHNITT_LEER = "BAENDER_OHNE_SCHNITT";
        /// <summary>Grund „masselos": R = 1/U − R_si − R_se ist nicht positiv.</summary>
        internal const string GRUND_R_NICHT_POSITIV = "R_NICHT_POSITIV";
        /// <summary>Grund „masselos": Die Gruppe trägt keine Kapazität.</summary>
        internal const string GRUND_KEINE_KAPAZITAET = "KEINE_KAPAZITAET";
        /// <summary>Grund „masselos": Der Bauteilweg der Zone lehnt ab; die Kapazität ist nicht bestimmbar.</summary>
        internal const string GRUND_BAUTEILWEG = "BAUTEILWEG";
        /// <summary>Grund „masselos": Trennfläche zur Nachbarzone — ihre Gruppe entscheidet der Mehrzonenlauf.</summary>
        internal const string GRUND_TRENNFLAECHE = "TRENNFLAECHE";

        // ==================================================================
        //  Festwerte
        // ==================================================================

        /// <summary>Spezifische Wärmekapazität der Ersatzschicht [J/(kgK)] (Datenaustauschkonzept 5.3).</summary>
        internal const double CP_ERSATZ_JKGK = 1000.0;

        /// <summary>Die Rohdichte, zu der die Dicke der Ersatzschicht bevorzugt wird [kg/m³] (Mauerwerk).</summary>
        internal const double RHO_VORZUG_KGM3 = 1500.0;

        /// <summary>
        /// Wärmeleitfähigkeit der Ersatzschicht innerer Masse ohne U-Wert [W/(mK)]: Die Innengruppe
        /// rechnet im Lauf ohne U (Klassenweg, R₁ = 1/(h_ms·A)); ein Wert wie Mauerwerk mittlerer
        /// Rohdichte trägt die Masse mit einem Widerstand, den das Zielwerkzeug für eine Innenwand
        /// erwartet.
        /// </summary>
        internal const double LAMBDA_INNEN_ERSATZ_WMK = 1.0;

        /// <summary>Kleinster Widerstand eines masselosen Stoffs [m²K/W], wenn R = 1/U − R_si − R_se nicht positiv ist.</summary>
        internal const double R_MASSELOS_MIN_M2KW = 0.001;

        /// <summary>Toleranz für „gleicher Azimut, gleiche Neigung" eines Wirts [°].</summary>
        internal const double LAGE_TOLERANZ_GRAD = 1e-6;

        /// <summary>Spezifische Wärmekapazität einer Luftschicht mit äquivalentem λ ohne Wert [J/(kgK)].</summary>
        internal const double CP_LUFTSCHICHT_JKGK = 1000.0;

        /// <summary>Die Kennung der Wasserzeichen- und Kopfzeilen in <c>Campus/Description</c> (Zeilentrenner).</summary>
        private const string ZEILE = "\n";

        /// <summary>Bildet den Plan (Klassenkopf). Wirft nicht; was sich nicht exportieren lässt, steht als Ablehnung darin.</summary>
        internal GebaeudeExportPlan Vorbereiten(GebaeudeExportSatz satz, GebaeudeExportProfil profil)
        {
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            return new Bauer(satz, profil).Bauen();
        }

        /// <summary>
        /// Schreibt einen schreibbaren Plan über den Schreiber des Profils; die Bilanz trägt die Meldungen
        /// des Plans. Ein abgelehnter Plan ist ein Programmfehler des Aufrufers.
        /// </summary>
        internal GebaeudeExportBilanz Schreiben(GebaeudeExportPlan plan, Stream ziel, GebaeudeExportProfil profil, CancellationToken abbruch)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (profil == null) throw new ArgumentNullException(nameof(profil));
            if (plan.Abgelehnt) throw new InvalidOperationException("Ein abgelehnter Exportplan wird nicht geschrieben.");
            GebaeudeExportBilanz b = profil.SchreiberErzeugen().Schreiben(plan.Abbild, ziel, profil, abbruch);
            return new GebaeudeExportBilanz(b.Flaechen, b.Oeffnungen, b.Aufbauten, b.Ersatzaufbauten,
                                            plan.Meldungen.Concat(b.Meldungen).ToList(), b.Bytes);
        }

        /// <summary>
        /// Die Ersatzschicht eines Bauteils (Datenaustauschkonzept 5.3, D10): mit c = 1000 J/(kgK) und
        /// R = 1/U − R_si − R_se gilt d ∈ [λ_min·R, λ_max·R] ∩ [κ/(ρ_max·c), κ/(ρ_min·c)] ∩ [0,001 m; 1,0 m];
        /// bevorzugt die Dicke zu ρ = 1 500 kg/m³, sonst die nächste Grenze; dann λ = d/R, ρ = κ/(d·c).
        /// Ohne U (Innengruppe) gilt das feste λ <see cref="LAMBDA_INNEN_ERSATZ_WMK"/>. Ein leerer Schnitt,
        /// R ≤ 0 oder κ ≤ 0 ergibt keine Schicht mit Masse (<see cref="Ersatzwerte.Masselos"/> samt Grund).
        /// </summary>
        /// <param name="u">Der U-Wert des Bauteils [W/(m²K)]; NaN = keiner (Innengruppe).</param>
        /// <param name="kappa">Die flächenbezogene Kapazität [J/(m²K)].</param>
        /// <param name="rSi">Innerer Übergangswiderstand [m²K/W].</param>
        /// <param name="rSe">Äußerer Übergangswiderstand [m²K/W].</param>
        internal static Ersatzwerte Ersatzschicht(double u, double kappa, double rSi, double rSe)
        {
            double c = CP_ERSATZ_JKGK;
            bool mitU = !double.IsNaN(u);
            double r = mitU ? 1.0 / u - rSi - rSe : double.NaN;
            if (mitU && !(r > 0.0))
                return Ersatzwerte.OhneMasse(GRUND_R_NICHT_POSITIV, R_MASSELOS_MIN_M2KW);
            if (!(kappa > 0.0) || double.IsInfinity(kappa))
                return Ersatzwerte.OhneMasse(GRUND_KEINE_KAPAZITAET, mitU ? r : R_MASSELOS_MIN_M2KW);

            double unten = Math.Max(GebaeudeFestwerte.SCHICHT_DICKE_MIN_M, kappa / (GebaeudeFestwerte.ROHDICHTE_MAX_KGM3 * c));
            double oben = Math.Min(GebaeudeFestwerte.SCHICHT_DICKE_MAX_M, kappa / (GebaeudeFestwerte.ROHDICHTE_MIN_KGM3 * c));
            if (mitU)
            {
                unten = Math.Max(unten, GebaeudeFestwerte.LAMBDA_MIN_WMK * r);
                oben = Math.Min(oben, GebaeudeFestwerte.LAMBDA_MAX_WMK * r);
            }
            if (!(unten <= oben))
                return Ersatzwerte.OhneMasse(GRUND_SCHNITT_LEER, mitU ? r : R_MASSELOS_MIN_M2KW);

            double d = Math.Min(oben, Math.Max(unten, kappa / (RHO_VORZUG_KGM3 * c)));
            // An einer Bandgrenze kann der Quotient um eine Stelle hinter dem Komma herausfallen; die
            // Werte werden auf das Band gelegt, sonst lehnte der Rückimport die Schicht ab.
            double lambda = mitU ? Band(d / r, GebaeudeFestwerte.LAMBDA_MIN_WMK, GebaeudeFestwerte.LAMBDA_MAX_WMK) : LAMBDA_INNEN_ERSATZ_WMK;
            double rho = Band(kappa / (d * c), GebaeudeFestwerte.ROHDICHTE_MIN_KGM3, GebaeudeFestwerte.ROHDICHTE_MAX_KGM3);
            return new Ersatzwerte(true, null, d, lambda, rho, c, mitU ? r : d / lambda);
        }

        private static double Band(double wert, double unten, double oben) => Math.Min(oben, Math.Max(unten, wert));

        /// <summary>Die Werte einer Ersatzschicht: mit Masse (Dicke, λ, ρ, c) oder masselos (nur R) samt Grund.</summary>
        internal readonly record struct Ersatzwerte(bool MitMasse, string Grund, double DickeM, double LambdaWmK,
                                                    double RhoKgM3, double CpJkgK, double RM2KW)
        {
            internal bool Masselos => !MitMasse;

            internal static Ersatzwerte OhneMasse(string grund, double r)
                => new Ersatzwerte(false, grund, double.NaN, double.NaN, double.NaN, double.NaN, r);
        }

        // ==================================================================
        //  Der Bauer — der Zustand eines Laufs
        // ==================================================================

        private sealed class Bauer
        {
            private readonly GebaeudeExportSatz _satz;
            private readonly GebaeudeExportProfil _profil;
            private readonly List<PruefMeldung> _meldungen = new List<PruefMeldung>();
            private PruefMeldung _ablehnung;

            private GbxmlAbbild _abbild;
            private AbbildGebaeude _g;
            private ProjektGebaeudeModel _geb;
            private int _gebId;
            private bool _klassenweg;
            private List<ZoneModel> _zonen;
            private AbbildRaum _platzhalter;
            private readonly Dictionary<int, string> _raumJeZone = new Dictionary<int, string>();
            private readonly Dictionary<string, AbbildAufbau> _aufbauten = new Dictionary<string, AbbildAufbau>(StringComparer.Ordinal);
            private int _ersatz;
            private string _vorbehalt;

            internal Bauer(GebaeudeExportSatz satz, GebaeudeExportProfil profil)
            {
                _satz = satz;
                _profil = profil;
            }

            internal GebaeudeExportPlan Bauen()
            {
                try
                {
                    Bilden();
                }
                catch (Abgelehnt)
                {
                    // Die Ablehnung steht in _ablehnung.
                }
                return new GebaeudeExportPlan(_abbild, _meldungen, _ablehnung);
            }

            private void Bilden()
            {
                if (_satz?.Gebaeude == null || !(_satz.Gebaeude.ID_Gebaeude > 0)) Ablehnen(KEIN_GEBAEUDE);
                _geb = _satz.Gebaeude;
                _gebId = _geb.ID_Gebaeude;
                _vorbehalt = T("GEXP_DATEI_VORBEHALT");
                _klassenweg = _satz.Klassenweg;

                if (_klassenweg)
                {
                    GebaeudeZonenCtrl.Uebernahmevorschlag u = _satz.Uebernahme;
                    foreach (string eintrag in _satz.Uebernahmeprotokoll ?? Array.Empty<string>())
                        Info(UEBERNAHME_PROTOKOLL, eintrag);
                    Info(UEBERNAHME_LAUFZEIT, Math.Round(_satz.UebernahmeLaufzeitMs).ToString(CultureInfo.InvariantCulture));
                    if (u == null || !u.Ok || u.Zone == null) Ablehnen(UEBERNAHME_ABGELEHNT, u?.Meldung ?? "");
                    _zonen = new List<ZoneModel> { u.Zone };
                    Info(KLASSENWEG, Zahl(u.Faktor), Grundlage(u));
                }
                else _zonen = _satz.Zonen.ToList();

                _abbild = new GbxmlAbbild
                {
                    CampusKennung = GebaeudeExportKennung.Campus(_gebId),
                    Plz = _satz.Plz,
                    NordwinkelGrad = _satz.Plz != null ? 0.0 : (double?)null,
                };
                Gebaeudetypwahl art = _profil.Gebaeudetyp(_geb.Gebaeudeart);
                if (!art.Bekannt) Info(GEBAEUDEART_UNBEKANNT, _geb.Gebaeudeart ?? "");
                _g = new AbbildGebaeude
                {
                    Kennung = GebaeudeExportKennung.Gebaeude(_gebId),
                    Name = string.IsNullOrWhiteSpace(_geb.Gebaeudename) ? null : _geb.Gebaeudename.Trim(),
                    Art = art.Wert,
                    Beschreibung = Campusbeschreibung(),
                };
                _abbild.Gebaeude.Add(_g);
                if (_profil.Testlizenz) Info(TESTLIZENZ);
                if (_satz.Plz == null) Info(OHNE_ORT);

                // Erst alle Räume, damit eine Trennfläche den Raum ihrer Nachbarzone kennt.
                for (int i = 0; i < _zonen.Count; i++)
                {
                    ZoneModel z = _zonen[i];
                    string raum = _klassenweg ? GebaeudeExportKennung.KlassenRaum(_gebId) : Schluessel(() => GebaeudeExportKennung.Raum(z.ID), z.Bezeichner);
                    _raumJeZone[z.ID] = raum;
                    _g.Raeume.Add(Raum(z, raum));
                }
                for (int i = 0; i < _zonen.Count; i++) Zone(_zonen[i]);
                if (_platzhalter != null) _g.Raeume.Add(_platzhalter);

                int flaechen = _g.Bauteile.Count;
                if (flaechen < GbxmlVokabular.MINDESTZAHL_FLAECHEN)
                    Ablehnen(ZU_WENIG_FLAECHEN, flaechen.ToString(CultureInfo.InvariantCulture),
                             GbxmlVokabular.MINDESTZAHL_FLAECHEN.ToString(CultureInfo.InvariantCulture));
                if (_ersatz > 0) Info(ERSATZSCHICHTUNG, _ersatz.ToString(CultureInfo.InvariantCulture), _vorbehalt);

                IReadOnlyList<string> verluste = GebaeudeExportVerluste.Getragen(_geb, _zonen);
                if (verluste.Count > 0) Info(VERLUSTE, string.Join(", ", verluste));
            }

            // --------------------------------------------------------------
            //  Kopf
            // --------------------------------------------------------------

            private string Campusbeschreibung()
            {
                var zeilen = new List<string>();
                if (_profil.Testlizenz) zeilen.Add(T("GEXP_DATEI_TESTVERSION"));
                zeilen.Add(T("GEXP_DATEI_STUFE"));
                zeilen.Add(T(nameof(MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007)));
                if (_klassenweg && _satz.Uebernahme != null)
                    zeilen.Add(Format("GEXP_DATEI_KLASSENWEG", _satz.Uebernahme.Faktor.ToString("0.####", _profil.Sprache), Grundlage(_satz.Uebernahme)));
                if (_satz.Plz == null) zeilen.Add(T("GEXP_DATEI_OHNE_ORT"));
                if (!string.IsNullOrWhiteSpace(_satz.Klimaregion)) zeilen.Add(Format("GEXP_DATEI_KLIMAREGION", _satz.Klimaregion));
                return string.Join(ZEILE, zeilen);
            }

            private string Grundlage(GebaeudeZonenCtrl.Uebernahmevorschlag u)
                => T(u.Verbrauchsangabe ? "GEXP_DATEI_GRUNDLAGE_VERBRAUCH" : "GEXP_DATEI_GRUNDLAGE_FLAECHE");

            // --------------------------------------------------------------
            //  Räume und Zonen — wirksame Werte
            // --------------------------------------------------------------

            /// <summary>Die Nutzfläche der Zone wie im Lauf: ihre eigene, sonst die des Gebäudes.</summary>
            private double Flaeche(ZoneModel z) => z.Nutzflaeche ?? _geb.Nutzflaeche;

            /// <summary>Die Raumhöhe der Zone: ihre eigene, sonst die des Gebäudes.</summary>
            private double Hoehe(ZoneModel z) => z.Raumhoehe ?? _geb.Raumhoehe;

            private bool Gekuehlt(ZoneModel z) => _satz.Kuehlbetrieb && (z.Kuehlung_Aktiv ?? _geb.Kuehlung_Aktiv);

            private AbbildRaum Raum(ZoneModel z, string kennung)
            {
                double a = Flaeche(z);
                double v = z.Volumen ?? a * Hoehe(z);
                // Die inneren Gewinne der Zone: eigener Wert, sonst der Anteil des Gebäudes nach dem
                // Flächenschlüssel (wie GebaeudeModellEingang); je Fläche als Mittelwert ohne Zeitplan (E43).
                double gewinne = z.Interne_Waermegewinne ?? (_geb.Nutzflaeche > 0.0 ? _geb.Interne_Waermegewinne * a / _geb.Nutzflaeche : 0.0);
                double? personen = z.Bewohner ?? (_geb.Flaeche_Nutzer > 0.0 ? a / _geb.Flaeche_Nutzer : (double?)null);
                bool gekuehlt = z.IstBeheizt && Gekuehlt(z);
                return new AbbildRaum
                {
                    Kennung = kennung,
                    Quelltyp = "Space",
                    Name = string.IsNullOrWhiteSpace(z.Bezeichner) ? null : z.Bezeichner.Trim(),
                    FlaecheM2 = a > 0.0 ? a : (double?)null,
                    VolumenM3 = v > 0.0 ? v : (double?)null,
                    Beheizt = z.IstBeheizt,
                    Zustandsangabe = !z.IstBeheizt ? GbxmlVokabular.Unconditioned : gekuehlt ? GbxmlVokabular.HeatedAndCooled : GbxmlVokabular.Heated,
                    LuftwechselJeH = z.Luftwechsel_Infiltration ?? _geb.Luftwechsel_Infiltration ?? GebaeudeFestwerte.VORGABE_LUFTWECHSEL_INFILTRATION,
                    Personen = personen > 0.0 ? personen : null,
                    GeraeteWm2 = a > 0.0 ? gewinne / a : (double?)null,
                    SollHeizenC = z.Raumsolltemperatur_Tag ?? _geb.Raumsolltemperatur_Tag,
                    SollKuehlenC = gekuehlt ? z.Kuehl_Sollwert ?? _geb.Kuehl_Sollwert : null,
                    ZonenKennung = _klassenweg ? GebaeudeExportKennung.KlassenZone(_gebId) : GebaeudeExportKennung.Zone(z.ID),
                    Beschreibung = T("GEXP_DATEI_MITTELWERT"),
                    ZonenBeschreibung = T(nameof(MyResource.Resource.GEB_PRODUKTAUSWEIS_VDI6007)),
                };
            }

            private string Platzhalter()
            {
                _platzhalter ??= new AbbildRaum
                {
                    Kennung = GebaeudeExportKennung.Unbeheizt(_gebId),
                    Quelltyp = "Space",
                    Name = T("GEXP_DATEI_UNBEHEIZT"),
                    Beheizt = false,
                    Zustandsangabe = GbxmlVokabular.Unconditioned,
                };
                return _platzhalter.Kennung;
            }

            // --------------------------------------------------------------
            //  Die Bauteile einer Zone
            // --------------------------------------------------------------

            /// <summary>Eine EPOS-Zeile mit ihren wirksamen Werten.</summary>
            private sealed class Zeile
            {
                internal BauteilModel B;
                internal BauteilEingang E;
                internal Umkehrzelle Zelle;
                internal Waermestromrichtung Richtung;
                internal int Rang;
                internal string Kennung;
                internal bool Oeffnung;
                internal readonly List<Zeile> Oeffnungen = new List<Zeile>();
                internal string Name => string.IsNullOrWhiteSpace(B.Bezeichner) ? "#" + Rang.ToString(CultureInfo.InvariantCulture) : B.Bezeichner.Trim();
            }

            private void Zone(ZoneModel z)
            {
                string raum = _raumJeZone[z.ID];
                double hoehe = Hoehe(z);
                var zeilen = new List<Zeile>();
                List<BauteilModel> bauteile = (z.Bauteile ?? new List<BauteilModel>()).Where(b => b != null).ToList();
                for (int i = 0; i < bauteile.Count; i++) zeilen.Add(Lesen(bauteile[i], i + 1, z));

                // Öffnungen in ihren Wirt.
                List<Zeile> opak = zeilen.Where(x => !x.Oeffnung && x.E.Rand != Bauteilrand.Innen && x.E.Rand != Bauteilrand.Zone).ToList();
                foreach (Zeile o in zeilen.Where(x => x.Oeffnung))
                {
                    Zeile wirt = opak.FirstOrDefault(w => w.E.Rand == o.E.Rand && GleicheLage(w, o))
                                 ?? opak.FirstOrDefault(w => w.E.Rand == o.E.Rand && w.Richtung == o.Richtung);
                    if (wirt == null) Ablehnen(OEFFNUNG_OHNE_WIRT, o.Name);
                    if (!GleicheLage(wirt, o)) Warnung(OEFFNUNG_ERSATZWIRT, o.Name, wirt.Name);
                    wirt.Oeffnungen.Add(o);
                }

                // Wechsel der Umkehrtabelle nennen.
                foreach (Zeile x in zeilen.Where(x => x.Zelle.Ergebnis == Umkehrergebnis.Wechsel))
                    Info(WECHSEL, x.Name, x.Zelle.Grund);

                // Die Kapazitäten, mit denen der Lauf die Gruppen rechnet (ohne Trennflächen: die ordnet
                // erst der Mehrzonenlauf einer Gruppe zu).
                Gruppenkapazitaeten? kap = null;
                string kapGrund = null;
                try
                {
                    double af = Flaeche(z);
                    var g = new BauteilwegGebaeude(z.Bezeichner ?? "", af,
                        _geb.Nutzflaeche > 0.0 ? _geb.Bauweise * af / _geb.Nutzflaeche : double.NaN,
                        _geb.Masseanteil_Aussen ?? GebaeudeFestwerte.VORGABE_MASSEANTEIL_AUSSEN,
                        _geb.Innenflaechenfaktor ?? GebaeudeFestwerte.VORGABE_INNENFLAECHENFAKTOR, 0.0);
                    kap = ErsatzparameterRC.GruppenkapazitaetenAusBauteilweg(g, zeilen.Where(x => x.E.Rand != Bauteilrand.Zone).Select(x => x.E).ToList());
                }
                catch (GebaeudeModellException ex)
                {
                    kapGrund = GRUND_BAUTEILWEG + ": " + ex.Message;
                }

                // Außengruppe ohne Schichten: κ = C_AW / Σ A (nur, wenn keine Außenschicht Masse trägt).
                List<Zeile> aussenOhne = zeilen.Where(x => x.E.Gruppe == Bauteilgruppe.Aussen && x.E.Rand != Bauteilrand.Zone && !x.E.HatSchichten).ToList();
                double kappaAussen = double.NaN;
                string grundAussen = kapGrund;
                if (kap is Gruppenkapazitaeten k1)
                {
                    if (k1.WegAussen == Gruppenweg.Klassenweg)
                    {
                        double summe = aussenOhne.Sum(x => x.E.Flaeche_M2);
                        kappaAussen = summe > 0.0 ? k1.C_AW_Jk / summe : 0.0;
                    }
                    else grundAussen = GRUND_GEMISCHT;
                }

                // Innengruppe ohne Schichten: κ = C_IW / Σ A bzw. / A_IW ohne Innenbauteile.
                List<Zeile> innen = zeilen.Where(x => x.E.Rand == Bauteilrand.Innen).ToList();
                double kappaInnen = double.NaN;
                string grundInnen = kapGrund;
                if (kap is Gruppenkapazitaeten k2)
                {
                    if (k2.WegInnen == Gruppenweg.Klassenweg)
                    {
                        double summe = innen.Count > 0 ? innen.Sum(x => x.E.Flaeche_M2) : k2.A_IW_M2;
                        kappaInnen = summe > 0.0 ? k2.C_IW_Jk / summe : 0.0;
                    }
                    else grundInnen = GRUND_GEMISCHT;
                }

                // Flächen: opake Bauteile samt Öffnungen, dann die Innenzeilen, dann die Ersatzfläche innerer Masse.
                foreach (Zeile x in zeilen.Where(x => !x.Oeffnung && x.E.Rand != Bauteilrand.Innen))
                {
                    double kappa = x.E.Rand == Bauteilrand.Zone ? double.NaN : kappaAussen;
                    string grund = x.E.Rand == Bauteilrand.Zone ? GRUND_TRENNFLAECHE : grundAussen;
                    _g.Bauteile.Add(Flaeche(x, raum, hoehe, x.B.Flaeche + x.Oeffnungen.Sum(o => o.B.Flaeche), kappa, grund,
                                            Nachbarn(x.Zelle, raum, x.B)));
                }
                Innenzeilen(innen, raum, hoehe, kappaInnen, grundInnen);
                if (innen.Count == 0 && kap is Gruppenkapazitaeten k3 && k3.WegInnen == Gruppenweg.Klassenweg && k3.A_IW_M2 > 0.0)
                    Innenmasse(z, raum, hoehe, k3.A_IW_M2, kappaInnen);
            }

            /// <summary>Liest eine Zeile mit den Funktionen des Laufs und ordnet sie in die Umkehrtabelle.</summary>
            private Zeile Lesen(BauteilModel b, int rang, ZoneModel z)
            {
                string name = string.IsNullOrWhiteSpace(b.Bezeichner) ? "#" + rang.ToString(CultureInfo.InvariantCulture) : b.Bezeichner.Trim();
                BauteilEingang e;
                Waermestromrichtung richtung;
                try
                {
                    e = GebaeudeZonenabbildung.AlsBauteil(b, _satz.Aufbauten, name)
                        .MitGebaeudewerten(_geb.Fensterdurchlassgrad, _geb.Rahmenanteil ?? GebaeudeFestwerte.VORGABE_RAHMENANTEIL,
                                           _geb.Verschattungsfaktor ?? GebaeudeFestwerte.VORGABE_VERSCHATTUNGSFAKTOR);
                    richtung = Bauteilreduktion.RichtungAusNeigung(e.NeigungWirksamGrad, name);
                }
                catch (GebaeudeModellException ex)
                {
                    Ablehnen(BAUTEIL_UNGUELTIG, name, ex.Message);
                    throw;   // nicht erreicht
                }

                Umkehrspalte spalte = GbxmlUmkehrung.SpalteAus(b.Randbedingung).Value;
                Umkehrzelle zelle = GbxmlUmkehrung.Zelle(e.Art, spalte, richtung);
                if (zelle.Ergebnis == Umkehrergebnis.Ablehnung) Ablehnen(UMKEHR_ABLEHNUNG, name, zelle.Grund);
                if (spalte == Umkehrspalte.Zone)
                {
                    if (!b.ID_Nachbarzone.HasValue) Ablehnen(UMKEHR_ABLEHNUNG, name, GbxmlUmkehrung.GRUND_ZONE);
                    if (!_raumJeZone.ContainsKey(b.ID_Nachbarzone.Value) || b.ID_Nachbarzone.Value == z.ID)
                        Ablehnen(NACHBARZONE_UNBEKANNT, name);
                }

                bool oeffnung = zelle.Oeffnungsart != null;
                string kennung = _klassenweg
                    ? GebaeudeExportKennung.KlassenBauteil(_gebId, rang)
                    : Schluessel(() => oeffnung ? GebaeudeExportKennung.Oeffnung(b.ID) : GebaeudeExportKennung.Bauteil(b.ID), name);
                if (b.U_Wert.HasValue && b.ID_Aufbau.HasValue && !oeffnung)
                    Warnung(U_NEBEN_AUFBAU, name, Zahl(b.U_Wert.Value));
                return new Zeile { B = b, E = e, Zelle = zelle, Richtung = richtung, Rang = rang, Kennung = kennung, Oeffnung = oeffnung };
            }

            private static bool GleicheLage(Zeile w, Zeile o)
            {
                if (w == null) return false;
                bool azimutGleich = double.IsNaN(w.E.AzimutGrad) && double.IsNaN(o.E.AzimutGrad)
                                    || Math.Abs(w.E.AzimutGrad - o.E.AzimutGrad) <= LAGE_TOLERANZ_GRAD;
                return azimutGleich && Math.Abs(w.E.NeigungWirksamGrad - o.E.NeigungWirksamGrad) <= LAGE_TOLERANZ_GRAD;
            }

            /// <summary>Die Nachbarn einer Fläche nach der Umkehrtabelle — der eigene Raum zuerst, mit seiner Sicht.</summary>
            private List<AbbildNachbar> Nachbarn(Umkehrzelle zelle, string raum, BauteilModel b)
            {
                var n = new List<AbbildNachbar> { new AbbildNachbar(raum, zelle.Sicht) };
                switch (zelle.Nachbarn)
                {
                    case Umkehrnachbarn.MitPlatzhalter: n.Add(new AbbildNachbar(Platzhalter(), null)); break;
                    case Umkehrnachbarn.InnenBeidseitig: n.Add(new AbbildNachbar(raum, zelle.GegenSicht)); break;
                    case Umkehrnachbarn.Nachbarzone: n.Add(new AbbildNachbar(_raumJeZone[b.ID_Nachbarzone.Value], zelle.GegenSicht)); break;
                }
                return n;
            }

            /// <summary>Eine opake Fläche samt ihren Öffnungen.</summary>
            private AbbildBauteil Flaeche(Zeile x, string raum, double hoehe, double brutto, double kappa, string grund, List<AbbildNachbar> nachbarn)
            {
                AbbildBauteil f = Bauteil(x.Kennung, "Surface", x.Name, x.Zelle.Flaechenart, x.E, brutto, x.Richtung, hoehe);
                f.Nachbarn.AddRange(nachbarn);
                f.Aufbau = Aufbau(x, kappa, grund, x.Zelle.Nachbarn == Umkehrnachbarn.Nachbarzone ? Bauteilrand.Zone : x.E.Rand);
                f.UWertWm2K = f.Aufbau.UWertWm2K;
                foreach (Zeile o in x.Oeffnungen) f.Oeffnungen.Add(Oeffnung(o, x, kappa, grund));
                return f;
            }

            private AbbildBauteil Oeffnung(Zeile o, Zeile wirt, double kappa, string grund)
            {
                AbbildBauteil b = Bauteil(o.Kennung, "Opening", o.Name, o.Zelle.Oeffnungsart, o.E, o.B.Flaeche, o.Richtung, double.NaN);
                bool fenster = o.E.IstTransparent;
                if (fenster)
                {
                    b.FenstertypKennung = _klassenweg ? GebaeudeExportKennung.KlassenFenstertyp(_gebId, o.Rang) : GebaeudeExportKennung.Fenstertyp(o.B.ID);
                    b.UWertWm2K = Endlich(o.E.UWert_WM2K);
                    b.GWert = Endlich(o.E.GWert);
                }
                else
                {
                    // Tür: U an der Öffnung; mit Schichten oder als Außenbauteil ohne Schichten ein Aufbau.
                    b.Aufbau = Aufbau(o, kappa, grund, o.E.Rand);
                    b.UWertWm2K = Endlich(o.E.UWert_WM2K) ?? b.Aufbau.UWertWm2K;
                }
                return b;
            }

            /// <summary>Ein Bauteil des Abbilds mit Lage und Rechteck (Breite × Höhe = Fläche).</summary>
            private static AbbildBauteil Bauteil(string kennung, string quelltyp, string name, string art, BauteilEingang e,
                                                double flaeche, Waermestromrichtung richtung, double hoehe)
            {
                double h = richtung == Waermestromrichtung.Horizontal && hoehe > 0.0 && !double.IsInfinity(hoehe) ? hoehe : Math.Sqrt(flaeche);
                return new AbbildBauteil
                {
                    Kennung = kennung,
                    Quelltyp = quelltyp,
                    Name = name,
                    Quellart = art,
                    Art = quelltyp == "Opening" ? GbxmlVokabular.Oeffnungsarten[art] : GbxmlVokabular.Flaechenarten[art].Art,
                    BruttoflaecheM2 = flaeche,
                    BreiteM = flaeche / h,
                    HoeheM = h,
                    AzimutGrad = Endlich(e.AzimutGrad),
                    NeigungGrad = e.NeigungWirksamGrad,
                };
            }

            // --------------------------------------------------------------
            //  Innenzeilen (E45) und Ersatzfläche innerer Masse
            // --------------------------------------------------------------

            private void Innenzeilen(List<Zeile> innen, string raum, double hoehe, double kappa, string grund)
            {
                var vergeben = new HashSet<Zeile>();
                foreach (Zeile a in innen)
                {
                    if (!vergeben.Add(a)) continue;
                    Zeile partner = innen.FirstOrDefault(b => !vergeben.Contains(b) && Partner(a, b));
                    double flaeche = a.B.Flaeche;
                    Zeile vorn = a;
                    if (partner != null)
                    {
                        vergeben.Add(partner);
                        if (partner.B.ID > 0 && partner.B.ID < a.B.ID) vorn = partner;
                    }
                    else
                    {
                        flaeche = a.B.Flaeche / 2.0;
                        Info(INNEN_HALBZEILE, a.Name);
                    }
                    _g.Bauteile.Add(Flaeche(vorn, raum, hoehe, flaeche, kappa, grund, Nachbarn(vorn.Zelle, raum, vorn.B)));
                }
            }

            /// <summary>Gleiche Art, gleiche Fläche (1e-9 relativ), gleicher oder gespiegelter Aufbau (ohne Aufbau: gleicher U-Wert).</summary>
            private bool Partner(Zeile a, Zeile b)
            {
                if (a.E.Art != b.E.Art) return false;
                if (Math.Abs(a.B.Flaeche - b.B.Flaeche) > 1e-9 * Math.Max(1.0, Math.Abs(a.B.Flaeche))) return false;
                if (a.B.ID_Aufbau == b.B.ID_Aufbau) return a.B.ID_Aufbau.HasValue || a.B.U_Wert == b.B.U_Wert;
                if (!a.B.ID_Aufbau.HasValue || !b.B.ID_Aufbau.HasValue) return false;
                if (!_satz.Aufbauten.TryGetValue(a.B.ID_Aufbau.Value, out BauteilaufbauModel x)
                    || !_satz.Aufbauten.TryGetValue(b.B.ID_Aufbau.Value, out BauteilaufbauModel y)) return false;
                List<BauteilschichtModel> sx = x.Schichten.OrderBy(s => s.Reihenfolge).ToList();
                List<BauteilschichtModel> sy = y.Schichten.OrderBy(s => s.Reihenfolge).Reverse().ToList();
                if (sx.Count != sy.Count) return false;
                for (int i = 0; i < sx.Count; i++)
                    if (sx[i].Dicke != sy[i].Dicke || sx[i].Lambda != sy[i].Lambda || sx[i].Rho != sy[i].Rho || sx[i].Cp != sy[i].Cp
                        || sx[i].IstLuftschicht != sy[i].IstLuftschicht) return false;
                return true;
            }

            /// <summary>Die Ersatzfläche innerer Masse einer Zone ohne Innenbauteile: A_IW/2, zweimal derselbe Raum.</summary>
            private void Innenmasse(ZoneModel z, string raum, double hoehe, double aIw, double kappa)
            {
                bool eineZone = _zonen.Count == 1;
                string kennung = eineZone ? GebaeudeExportKennung.Innenmasse(_gebId) : GebaeudeExportKennung.Innenmasse(_gebId) + "-" + Zonenschluessel(z);
                string aufbau = eineZone ? GebaeudeExportKennung.InnenmasseAufbau(_gebId) : GebaeudeExportKennung.InnenmasseAufbau(_gebId) + "-" + Zonenschluessel(z);
                string schicht = eineZone ? GebaeudeExportKennung.InnenmasseSchicht(_gebId) : GebaeudeExportKennung.InnenmasseSchicht(_gebId) + "-" + Zonenschluessel(z);
                string stoff = eineZone ? GebaeudeExportKennung.InnenmasseStoff(_gebId) : GebaeudeExportKennung.InnenmasseStoff(_gebId) + "-" + Zonenschluessel(z);
                const double NEIGUNG = 90.0;
                var e = new BauteilEingang(T("GEXP_DATEI_INNENMASSE"), Bauteilart.Innenwand, aIw, Bauteilrand.Innen, neigungGrad: NEIGUNG);
                Umkehrzelle zelle = GbxmlUmkehrung.Zelle(Bauteilart.Innenwand, Umkehrspalte.Leer, Waermestromrichtung.Horizontal);
                AbbildBauteil f = Bauteil(kennung, "Surface", e.Bezeichnung, zelle.Flaechenart, e, aIw / 2.0, Waermestromrichtung.Horizontal, hoehe);
                f.Nachbarn.Add(new AbbildNachbar(raum, zelle.Sicht));
                f.Nachbarn.Add(new AbbildNachbar(raum, zelle.GegenSicht));
                f.Aufbau = Ersatzaufbau(aufbau, schicht, stoff, e.Bezeichnung, double.NaN, kappa, Bauteilrand.Innen, NEIGUNG, null, e.Bezeichnung);
                f.UWertWm2K = f.Aufbau.UWertWm2K;
                _g.Bauteile.Add(f);
                Info(INNENMASSE, z.Bezeichner ?? "", Zahl(aIw));
            }

            private string Zonenschluessel(ZoneModel z) => z.ID > 0 ? z.ID.ToString(CultureInfo.InvariantCulture) : "klasse";

            // --------------------------------------------------------------
            //  Aufbauten
            // --------------------------------------------------------------

            /// <summary>Der Aufbau einer Zeile: je Übergangsfall aus den Schichten, sonst die Ersatzschichtung.</summary>
            private AbbildAufbau Aufbau(Zeile x, double kappa, string grund, Bauteilrand rand)
            {
                if (x.E.HatSchichten) return Schichtaufbau(x, rand);
                string aufbau, schicht, stoff;
                if (_klassenweg)
                {
                    aufbau = GebaeudeExportKennung.KlassenAufbau(_gebId, x.Rang);
                    schicht = GebaeudeExportKennung.KlassenSchicht(_gebId, x.Rang);
                    stoff = GebaeudeExportKennung.KlassenStoff(_gebId, x.Rang);
                }
                else
                {
                    aufbau = GebaeudeExportKennung.ErsatzAufbau(x.B.ID);
                    schicht = GebaeudeExportKennung.ErsatzSchicht(x.B.ID);
                    stoff = GebaeudeExportKennung.ErsatzStoff(x.B.ID);
                }
                return Ersatzaufbau(aufbau, schicht, stoff, x.Name, x.E.UWert_WM2K, kappa, rand, x.E.NeigungWirksamGrad, grund, x.Name);
            }

            /// <summary>Der Aufbau aus den Schichten der Zeile — EINER je Aufbau und Übergangsfall, mit dessen U-Wert.</summary>
            private AbbildAufbau Schichtaufbau(Zeile x, Bauteilrand rand)
            {
                int id = x.B.ID_Aufbau.Value;
                string kennung = GebaeudeExportKennung.Aufbau(id, x.Richtung, rand);
                if (_aufbauten.TryGetValue(kennung, out AbbildAufbau vorhanden)) return vorhanden;

                BauteilaufbauModel m = _satz.Aufbauten[id];
                double u;
                try
                {
                    u = Bauteilreduktion.UWertAusSchichten(x.E.Schichten, x.E.NeigungWirksamGrad, rand, x.Name).U_WM2K;
                }
                catch (GebaeudeModellException ex)
                {
                    Ablehnen(BAUTEIL_UNGUELTIG, x.Name, ex.Message);
                    throw;
                }
                var a = new AbbildAufbau
                {
                    Kennung = kennung,
                    Name = string.IsNullOrWhiteSpace(m.Bezeichner) ? null : m.Bezeichner.Trim(),
                    Beschreibung = string.IsNullOrWhiteSpace(m.Beschreibung) ? null : m.Beschreibung.Trim(),
                    UWertWm2K = u,
                    Richtung = Schichtrichtung.InnenNachAussen,
                    RichtungAngenommen = false,
                    Status = Aufbaustatus.Vollstaendig,
                };
                foreach (BauteilschichtModel s in m.Schichten.OrderBy(s => s.Reihenfolge).ThenBy(s => s.ID))
                    a.Schichten.Add(Schicht(m, s, x.Richtung));
                _aufbauten[kennung] = a;
                return a;
            }

            /// <summary>Eine Schicht mit ihrer Wertekopie; die Luftschichten nach den Regeln des Klassenkopfs.</summary>
            private AbbildSchicht Schicht(BauteilaufbauModel m, BauteilschichtModel s, Waermestromrichtung richtung)
            {
                string name = s.ID_Baustoff is int idB && _satz.Baustoffe.TryGetValue(idB, out BaustoffModel stoff) && !string.IsNullOrWhiteSpace(stoff.Bezeichner)
                    ? stoff.Bezeichner.Trim()
                    : s.IstLuftschicht ? T("GEXP_DATEI_LUFTSCHICHT") : Format("GEXP_DATEI_SCHICHT", s.Reihenfolge.ToString(CultureInfo.InvariantCulture));
                if (s.IstLuftschicht && !s.Lambda.HasValue)
                {
                    // Ruhende Luftschicht: Dicke und Widerstand nach Tabelle 8 in der Richtung dieses Falls
                    // (gemessen: der Namensabgleich trifft „Luftschicht" und rechnet d/R als äquivalentes λ).
                    return new AbbildSchicht
                    {
                        Kennung = GebaeudeExportKennung.SchichtRuhendeLuft(m.ID, s.Reihenfolge, richtung),
                        BaustoffKennung = GebaeudeExportKennung.StoffRuhendeLuft(m.ID, s.Reihenfolge, richtung),
                        Name = name,
                        DickeM = s.Dicke,
                        RWertM2KW = Bauteilreduktion.Luftschichtwiderstand(s.Dicke, richtung, m.Bezeichner, s.Reihenfolge),
                    };
                }
                double? rho = s.Rho, cp = s.Cp;
                if (s.IstLuftschicht && (!(rho >= GebaeudeFestwerte.ROHDICHTE_MIN_KGM3) || !cp.HasValue))
                {
                    // Luftschicht mit äquivalentem λ: ρ ≥ 5 kg/m³ (Band des Rückimports), c gesetzt.
                    rho = Math.Max(rho ?? 0.0, GebaeudeFestwerte.ROHDICHTE_MIN_KGM3);
                    cp ??= CP_LUFTSCHICHT_JKGK;
                    Info(LUFTSCHICHT_ANGEHOBEN, m.Bezeichner ?? "", s.Reihenfolge.ToString(CultureInfo.InvariantCulture));
                }
                return new AbbildSchicht
                {
                    Kennung = GebaeudeExportKennung.Schicht(m.ID, s.Reihenfolge),
                    BaustoffKennung = GebaeudeExportKennung.Stoff(m.ID, s.Reihenfolge),
                    Name = name,
                    DickeM = s.Dicke,
                    LambdaWmK = s.Lambda,
                    RhoKgM3 = rho,
                    CpJkgK = cp,
                };
            }

            /// <summary>Die gekennzeichnete Ersatzschichtung eines Bauteils ohne Schichten (D10).</summary>
            private AbbildAufbau Ersatzaufbau(string kennung, string schicht, string stoff, string bauteil, double u, double kappa,
                                              Bauteilrand rand, double neigung, string grundOhneKappa, string meldename)
            {
                (double rSi, double rSe) = Bauteilreduktion.Uebergangswiderstaende(neigung, rand == Bauteilrand.Zone ? Bauteilrand.Unbeheizt : rand);
                Ersatzwerte w = double.IsNaN(kappa)
                    ? Ersatzwerte.OhneMasse(grundOhneKappa ?? GRUND_KEINE_KAPAZITAET, double.IsNaN(u) ? R_MASSELOS_MIN_M2KW : Math.Max(1.0 / u - rSi - rSe, R_MASSELOS_MIN_M2KW))
                    : Ersatzschicht(u, kappa, rSi, rSe);
                if (w.Masselos && !double.IsNaN(u) && w.Grund != GRUND_R_NICHT_POSITIV && !(w.RM2KW > 0.0))
                    w = w with { RM2KW = R_MASSELOS_MIN_M2KW };
                var s = new AbbildSchicht { Kennung = schicht, BaustoffKennung = stoff };
                if (w.MitMasse)
                {
                    s.Name = Format("GEXP_DATEI_ERSATZ_STOFF", _vorbehalt);
                    s.DickeM = w.DickeM;
                    s.LambdaWmK = w.LambdaWmK;
                    s.RhoKgM3 = w.RhoKgM3;
                    s.CpJkgK = w.CpJkgK;
                }
                else
                {
                    s.Name = T("GEXP_DATEI_ERSATZ_MASSELOS");
                    s.RWertM2KW = w.RM2KW;
                    Warnung(MASSELOS, meldename, w.Grund ?? "");
                }
                double uAufbau = double.IsNaN(u) ? 1.0 / (rSi + w.RM2KW + rSe) : u;
                var a = new AbbildAufbau
                {
                    Kennung = kennung,
                    Name = Format("GEXP_DATEI_ERSATZ_AUFBAU", bauteil),
                    Beschreibung = Format("GEXP_DATEI_ERSATZ_BESCHREIBUNG", _vorbehalt),
                    UWertWm2K = uAufbau,
                    IstErsatz = true,
                    Richtung = Schichtrichtung.InnenNachAussen,
                    RichtungAngenommen = false,
                    Status = w.MitMasse ? Aufbaustatus.Vollstaendig : Aufbaustatus.Masselos,
                };
                a.Schichten.Add(s);
                _ersatz++;
                return a;
            }

            // --------------------------------------------------------------
            //  Hilfen
            // --------------------------------------------------------------

            private string Schluessel(Func<string> bilden, string name)
            {
                try
                {
                    return bilden();
                }
                catch (ArgumentOutOfRangeException)
                {
                    Ablehnen(BAUTEIL_UNGESPEICHERT, name ?? "");
                    throw;
                }
            }

            private string T(string schluessel)
                => MyResource.Resource.ResourceManager.GetString(schluessel, _profil.Sprache) ?? schluessel;

            private string Format(string schluessel, params object[] werte)
                => string.Format(_profil.Sprache, T(schluessel), werte);

            private static double? Endlich(double w) => double.IsNaN(w) || double.IsInfinity(w) ? (double?)null : w;

            private static string Zahl(double w) => w.ToString("R", CultureInfo.InvariantCulture);

            private void Info(string schluessel, params string[] werte) => _meldungen.Add(new PruefMeldung(PruefStufe.Info, schluessel, werte));

            private void Warnung(string schluessel, params string[] werte) => _meldungen.Add(new PruefMeldung(PruefStufe.Warnung, schluessel, werte));

            /// <summary>Die Ablehnung: eine Meldung der Stufe Fehler, dann Abbruch des Bildens.</summary>
            private void Ablehnen(string schluessel, params string[] werte)
            {
                _ablehnung = new PruefMeldung(PruefStufe.Fehler, schluessel, werte);
                _meldungen.Add(_ablehnung);
                throw new Abgelehnt();
            }

            private sealed class Abgelehnt : Exception
            {
            }
        }
    }
}
