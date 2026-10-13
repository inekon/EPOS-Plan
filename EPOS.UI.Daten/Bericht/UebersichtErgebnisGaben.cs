using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Seiten.Berichte;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Hülle des Abschnitts „Ergebnisse — Energie“ der Übersicht (Anwenderauftrag 13.10.2026): baut aus dem Stand der
    /// Seite (Gruppe, Vergleichswahl, Stamm) den Energievergleich des Kerns (<see cref="EnergievergleichCtrl"/>,
    /// <see cref="Energievergleich"/>) und bildet ihn auf die Typen der Razor-Komponente <c>UebersichtErgebnisse</c> ab.
    /// Die Kennzahlen je Version werden über die Sitzung gepuffert, solange sich ihr Simulationsstand nicht ändert.
    ///
    /// <para><b>Anzeige anpassen (Anwenderentscheid 13.10.2026).</b> Die Auflösung steht hier, an einer Stelle: Der
    /// Zustand einer Gruppe ist der Benutzerzustand aus <see cref="UebersichtErgebnisAnzeige.SCHLUESSEL_GRUPPEN"/>,
    /// sonst die Vorgabe des Kerns (<see cref="Energievergleich.GRUPPEN_DATEN"/>); abgewählte Kennzahlen
    /// (<see cref="UebersichtErgebnisAnzeige.SCHLUESSEL_KENNZAHLEN_AUS"/>) und unter
    /// <see cref="UebersichtErgebnisAnzeige.SCHLUESSEL_OHNE_WERT_AUS"/> die Zeilen ohne Wert nimmt die reine Funktion
    /// <see cref="UebersichtErgebnisAnzeige.Anwenden"/> aus der Gruppenliste — so gilt die Einstellung für Tabelle und
    /// Grafik gleich. Ob eine zugeklappte Gruppe gerade aufgeklappt ist, entscheidet allein die Sitzungsablage
    /// (<c>KlappgruppeOffenLesen</c>); gespeichert wird nur der Grundzustand, je Benutzer über
    /// <c>Dienste.Einstellungen</c>, für alle Projekte gleich.</para>
    /// </summary>
    internal sealed class UebersichtErgebnisGaben
    {
        /// <summary>Höchstens so viele Variantenspalten wie die Gegenüberstellung der Komponenten.</summary>
        internal const int MAX_VARIANTENSPALTEN = 8;

        /// <summary>
        /// Die aufgeklappten Klappgruppen (Gruppenschlüssel des Kerns) — ein Stand je Gruppe und Sitzung, wie „nur
        /// Unterschiede“.
        /// </summary>
        private static readonly HashSet<string> s_offeneKlappgruppen = new HashSet<string>();

        private readonly Func<int, bool, VariantenDaten> _lade;
        private readonly Dictionary<int, (string Stand, VariantenDaten Daten)> _puffer =
            new Dictionary<int, (string, VariantenDaten)>();

        private readonly IEinstellungen _einstellungen;

        /// <summary>Mit dem Ladeweg des Kerns.</summary>
        internal UebersichtErgebnisGaben() : this(EnergievergleichCtrl.Lade) { }

        /// <summary>
        /// Mit einem eigenen Ladeweg (Proben) und wahlweise einer eigenen Einstellungsablage (Attrappe der Naht);
        /// ohne sie gilt <c>Dienste.Einstellungen</c> zum Zeitpunkt des Zugriffs.
        /// </summary>
        internal UebersichtErgebnisGaben(Func<int, bool, VariantenDaten> lade, IEinstellungen einstellungen = null)
        {
            _lade = lade ?? throw new ArgumentNullException(nameof(lade));
            _einstellungen = einstellungen;
        }

        private IEinstellungen Ablage => _einstellungen ?? Dienste.Einstellungen;

        /// <summary>Der Parametersatz der Komponente — ohne <c>Stand</c>, den reicht die Seite.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Laden"] = new Func<UebersichtStand, UebersichtErgebnisStand>(Laden),
                ["Titel"] = MyResource.Resource.BK_ERG_ABSCHNITT_ERGEBNISSE,
                ["SpalteKennzahl"] = MyResource.Resource.BK_ERG_SP_KENNZAHL,
                ["LegendeAbweichung"] = MyResource.Resource.BK_LBL_LEGENDE_ABWEICHUNG,
                ["LegendeProzent"] = MyResource.Resource.BK_ERG_LEGENDE_PROZENT,
                ["HinweisQuelle"] = MyResource.Resource.BK_ERG_HINWEIS_QUELLE,
                ["KeineSimulationText"] = MyResource.Resource.BK_ERG_KEINE_SIMULATION,
                ["NichtSimuliertText"] = MyResource.Resource.BK_LBL_NICHT_SIMULIERT,
                ["WeichtAbText"] = MyResource.Resource.BK_LBL_WEICHT_AB,
                ["KeineUnterschiedeText"] = MyResource.Resource.BK_ERG_KEINE_UNTERSCHIEDE,
                ["AnzahlFormat"] = MyResource.Resource.BK_ERG_ANZAHL_KENNZAHLEN,
                ["AnzahlEins"] = MyResource.Resource.BK_ERG_ANZAHL_KENNZAHL_EINS,
                ["AnzeigeLesen"] = new Func<ErgebnisAnzeigeEinstellung>(AnzeigeLesen),
                ["AnzeigeSpeichern"] = new Action<ErgebnisAnzeigeEinstellung>(AnzeigeSpeichern),
                ["AnpassenTexte"] = AnpassenTexte(),
                ["HilfeSchluessel"] = "UcBkUebersicht.btn_Help",
                ["KlappgruppeOffenLesen"] = new Func<string, bool>(g => s_offeneKlappgruppen.Contains(g)),
                ["KlappgruppeOffenGewechselt"] = new Action<string, bool>((g, an) =>
                {
                    if (an) s_offeneKlappgruppen.Add(g);
                    else s_offeneKlappgruppen.Remove(g);
                })
            };
        }

        /// <summary>
        /// Der Energievergleich zum Stand der Seite: der Stamm und die Varianten der Vergleichswahl in der Reihenfolge der
        /// Gruppe, höchstens <see cref="MAX_VARIANTENSPALTEN"/> Varianten — dieselben Spalten wie die Gegenüberstellung.
        /// </summary>
        internal UebersichtErgebnisStand Laden(UebersichtStand stand)
        {
            var erg = new UebersichtErgebnisStand();
            VarianteZeile stamm = stand?.Zeilen.FirstOrDefault(z => z.IstStamm);
            if (stamm == null) return erg;

            List<VarianteZeile> versionen = new List<VarianteZeile> { stamm };
            versionen.AddRange(stand.Zeilen
                .Where(z => !z.IstStamm && stand.GewaehlteVarianten.Contains(z.IdProjekt))
                .Take(MAX_VARIANTENSPALTEN));

            erg.Versionsspalten = versionen.Select(v => new VersionsSpalte
            {
                Name = v.IstStamm ? MyResource.Resource.BK_SP_WERT_STAMM
                                  : (string.IsNullOrEmpty(v.Bezeichner) ? v.Projektname : v.Bezeichner),
                Stand = string.IsNullOrEmpty(v.SimZeitpunkt)
                    ? MyResource.Resource.BK_LBL_NICHT_SIMULIERT
                    : string.Format(MyResource.Resource.BK_LBL_SIMULIERT_AM, v.SimZeitpunkt)
            }).ToList();

            List<VariantenDaten> daten = versionen.Select(Daten).ToList();
            EnergievergleichErgebnis vergleich = Energievergleich.Vergleiche(daten, Sprache.Englisch, CultureInfo.CurrentCulture);
            erg.KeineSimulation = vergleich.KeineSimuliert;
            erg.Simuliert = vergleich.Simuliert.ToList();
            ErgebnisAnzeigeAuswertung anzeige = UebersichtErgebnisAnzeige.Anwenden(Abbilden(vergleich), AnzeigeLesen());
            erg.Gruppen = anzeige.Gruppen;
            erg.OhneWertAusgeblendet = anzeige.OhneWertAusgeblendet;
            string modus = EmissionsAusweis.ModusAusVarianten(daten.Where(v => v != null && v.Ergebnis != null));
            erg.Anpassbar = Anpassbar(vergleich, modus);
            return erg;
        }

        // ---- Anzeige anpassen -------------------------------------------------------------------------------------

        /// <summary>Die Vorgabe des Kerns je Gruppe als Zustand.</summary>
        internal static IReadOnlyDictionary<string, ErgebnisGruppenZustand> Vorgaben()
            => Energievergleich.GRUPPEN_DATEN.ToDictionary(g => g.Schluessel,
                g => g.VorgabeOffen ? ErgebnisGruppenZustand.Offen : ErgebnisGruppenZustand.Zugeklappt, StringComparer.Ordinal);

        /// <summary>Die gespeicherte Einstellung des Benutzers; die Vorgabe, wenn nichts hinterlegt oder nichts lesbar ist.</summary>
        internal ErgebnisAnzeigeEinstellung AnzeigeLesen()
        {
            try
            {
                IEinstellungen a = Ablage;
                if (a == null) return ErgebnisAnzeigeEinstellung.Standard();
                return UebersichtErgebnisAnzeige.AusText(a.Lies(UebersichtErgebnisAnzeige.SCHLUESSEL_GRUPPEN, ""),
                                                         a.Lies(UebersichtErgebnisAnzeige.SCHLUESSEL_KENNZAHLEN_AUS, ""),
                                                         a.LiesZahl(UebersichtErgebnisAnzeige.SCHLUESSEL_OHNE_WERT_AUS, 1),
                                                         Vorgaben());
            }
            catch
            {
                return ErgebnisAnzeigeEinstellung.Standard();
            }
        }

        /// <summary>
        /// Schreibt den Grundzustand aus dem Dialog (nach OK). Gleicht er der Vorgabe, verschwinden die drei Schlüssel.
        /// Eine Ablage, die nicht schreiben kann, lässt die Anzeige beim alten Stand — kein Abbruch.
        /// </summary>
        internal void AnzeigeSpeichern(ErgebnisAnzeigeEinstellung e)
        {
            if (e == null) return;
            try
            {
                IEinstellungen a = Ablage;
                if (a == null) return;
                if (e.IstStandard)
                {
                    a.Loesche(UebersichtErgebnisAnzeige.SCHLUESSEL_GRUPPEN);
                    a.Loesche(UebersichtErgebnisAnzeige.SCHLUESSEL_KENNZAHLEN_AUS);
                    a.Loesche(UebersichtErgebnisAnzeige.SCHLUESSEL_OHNE_WERT_AUS);
                    return;
                }
                a.Schreib(UebersichtErgebnisAnzeige.SCHLUESSEL_GRUPPEN, UebersichtErgebnisAnzeige.GruppenText(e));
                a.Schreib(UebersichtErgebnisAnzeige.SCHLUESSEL_KENNZAHLEN_AUS, UebersichtErgebnisAnzeige.KennzahlenText(e));
                a.SchreibZahl(UebersichtErgebnisAnzeige.SCHLUESSEL_OHNE_WERT_AUS, e.OhneWertAus ? 1 : 0);
            }
            catch
            {
                // Die Einstellung ist eine Bequemlichkeit; ein Schreibfehler der Ablage hält die Seite nicht an.
            }
        }

        /// <summary>
        /// Die Gruppen des Dialogs in Anzeigereihenfolge: je Gruppe die Kennzahlen des Katalogs (Modus wie der Vergleich)
        /// und die Trägerzeilen der aktuellen Daten.
        /// </summary>
        internal static IReadOnlyList<ErgebnisAnpassGruppe> Anpassbar(EnergievergleichErgebnis vergleich, string modus)
        {
            var jeGruppe = Energievergleich.GRUPPEN.ToDictionary(g => g, g => new List<ErgebnisAnpassKennzahl>());
            var bekannt = new HashSet<string>(StringComparer.Ordinal);
            foreach (Kennzahl k in Energievergleich.Kennzahlen(modus))
            {
                if (!jeGruppe.TryGetValue(Energievergleich.Gruppe(k), out var liste) || !bekannt.Add(k.Schluessel)) continue;
                string einheit = k.Einheit == "–" ? "" : (k.Einheit ?? "");
                string text = k.Label(Sprache.Englisch);
                liste.Add(new ErgebnisAnpassKennzahl { Schluessel = k.Schluessel, Bezeichnung = einheit.Length == 0 ? text : text + " [" + einheit + "]" });
            }
            foreach (EnergievergleichZeile z in vergleich?.Zeilen ?? new List<EnergievergleichZeile>())
            {
                if (!jeGruppe.TryGetValue(z.Gruppe, out var liste) || !bekannt.Add(z.Schluessel)) continue;
                liste.Add(new ErgebnisAnpassKennzahl
                {
                    Schluessel = z.Schluessel,
                    Bezeichnung = z.Einheit.Length == 0 ? z.Bezeichnung : z.Bezeichnung + " [" + z.Einheit + "]"
                });
            }
            return Energievergleich.GRUPPEN_DATEN.Select(g => new ErgebnisAnpassGruppe
            {
                Schluessel = g.Schluessel,
                Titel = Gruppentitel(g.Schluessel),
                Vorgabe = g.VorgabeOffen ? ErgebnisGruppenZustand.Offen : ErgebnisGruppenZustand.Zugeklappt,
                Kennzahlen = jeGruppe[g.Schluessel]
            }).ToList();
        }

        /// <summary>Die Beschriftungen des Dialogs „Anzeige anpassen…“ aus den Ressourcen.</summary>
        internal static ErgebnisAnpassenTexte AnpassenTexte() => new ErgebnisAnpassenTexte
        {
            Knopf = MyResource.Resource.BK_ERG_ANP_KNOPF,
            Titel = MyResource.Resource.BK_ERG_ANP_TITEL,
            Hinweis = MyResource.Resource.BK_ERG_ANP_HINWEIS,
            Offen = MyResource.Resource.BK_ERG_ANP_OFFEN,
            Zugeklappt = MyResource.Resource.BK_ERG_ANP_ZUGEKLAPPT,
            Ausgeblendet = MyResource.Resource.BK_ERG_ANP_AUSGEBLENDET,
            Kennzahlen = MyResource.Resource.BK_ERG_ANP_KENNZAHLEN,
            KennzahlenAnzahl = MyResource.Resource.BK_ERG_ANP_KENNZAHLEN_ANZAHL,
            OhneWert = MyResource.Resource.BK_ERG_ANP_OHNE_WERT,
            OhneWertErklaerung = MyResource.Resource.BK_ERG_ANP_OHNE_WERT_ERKL,
            Standard = MyResource.Resource.BK_ERG_ANP_STANDARD,
            OhneWertHinweis = MyResource.Resource.BK_ERG_ANP_OHNE_WERT_HINWEIS,
            OhneWertHinweisEins = MyResource.Resource.BK_ERG_ANP_OHNE_WERT_HINWEIS_EINS,
            Ok = MyResource.Resource.SIM_BTN_OK,
            Abbrechen = MyResource.Resource.SIM_BTN_ABBRECHEN
        };

        /// <summary>Die Kennzahlen einer Version — aus dem Puffer, solange ihr Simulationsstand derselbe ist.</summary>
        private VariantenDaten Daten(VarianteZeile v)
        {
            string stand = v.SimZeitpunkt ?? "";
            if (_puffer.TryGetValue(v.IdProjekt, out var p) && p.Stand == stand) return p.Daten;
            VariantenDaten d = stand.Length == 0 ? new VariantenDaten { IdProjekt = v.IdProjekt, IstStamm = v.IstStamm }
                                                 : _lade(v.IdProjekt, v.IstStamm);
            _puffer[v.IdProjekt] = (stand, d);
            return d;
        }

        /// <summary>Die Zeilen des Kerns als Gruppen der Razor-Komponente.</summary>
        internal static IReadOnlyList<ErgebnisVergleichGruppe> Abbilden(EnergievergleichErgebnis vergleich)
        {
            var gruppen = new List<ErgebnisVergleichGruppe>();
            foreach (string g in Energievergleich.GRUPPEN)
            {
                List<ErgebnisVergleichZeile> zeilen = vergleich.Zeilen.Where(z => z.Gruppe == g).Select(z =>
                    new ErgebnisVergleichZeile
                    {
                        Schluessel = z.Schluessel,
                        Bezeichnung = z.Einheit.Length == 0 ? z.Bezeichnung : z.Bezeichnung + " [" + z.Einheit + "]",
                        OhneWert = UebersichtErgebnisAnzeige.OhneWert(z.Werte),
                        Zellen = z.Texte.Select((t, i) => new ErgebnisVergleichZelle
                        {
                            Text = t,
                            WeichtAb = z.WeichtAb[i],
                            Abweichung = z.Abweichung[i],
                            NichtSimuliert = !vergleich.Simuliert[i]
                        }).ToList()
                    }).ToList();
                if (zeilen.Count > 0)
                    gruppen.Add(new ErgebnisVergleichGruppe
                    {
                        Schluessel = g,
                        Titel = Gruppentitel(g),
                        Zeilen = zeilen,
                        // Die Vorgabe des Kerns; den Benutzerzustand setzt UebersichtErgebnisAnzeige.Anwenden.
                        Zustand = Energievergleich.VorgabeOffen(g) ? ErgebnisGruppenZustand.Offen
                                                                   : ErgebnisGruppenZustand.Zugeklappt
                    });
            }
            return gruppen;
        }

        /// <summary>Der Anzeigetitel eines Gruppenschlüssels.</summary>
        internal static string Gruppentitel(string gruppe)
        {
            switch (gruppe)
            {
                case Energievergleich.GRUPPE_BEDARF: return MyResource.Resource.BK_ERG_GR_BEDARF;
                case Energievergleich.GRUPPE_ERZEUGUNG: return MyResource.Resource.BK_ERG_GR_ERZEUGUNG;
                case Energievergleich.GRUPPE_STROM: return MyResource.Resource.BK_ERG_GR_STROM;
                case Energievergleich.GRUPPE_ENDENERGIE: return MyResource.Resource.BK_ERG_GR_ENDENERGIE;
                case Energievergleich.GRUPPE_DECKUNG: return MyResource.Resource.BK_ERG_GR_DECKUNG;
                case Energievergleich.GRUPPE_KAELTE: return MyResource.Resource.BK_ERG_GR_KAELTE;
                case Energievergleich.GRUPPE_EFFIZIENZ: return MyResource.Resource.BK_ERG_GR_EFFIZIENZ;
                case Energievergleich.GRUPPE_EFFIZIENZ_WEITERE: return MyResource.Resource.BK_ERG_GR_EFFIZIENZ_WEITERE;
                case Energievergleich.GRUPPE_GEBAEUDE: return MyResource.Resource.BK_ERG_GR_GEBAEUDE;
                case Energievergleich.GRUPPE_EMISSIONEN: return MyResource.Resource.BK_ERG_GR_EMISSIONEN;
                default: return gruppe;
            }
        }
    }
}
