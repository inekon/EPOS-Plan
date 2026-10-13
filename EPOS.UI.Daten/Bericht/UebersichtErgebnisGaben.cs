using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Seiten.Berichte;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Hülle des Abschnitts „Ergebnisse — Energie“ der Übersicht (Anwenderauftrag 13.10.2026): baut aus dem Stand der
    /// Seite (Gruppe, Vergleichswahl, Stamm) den Energievergleich des Kerns (<see cref="EnergievergleichCtrl"/>,
    /// <see cref="Energievergleich"/>) und bildet ihn auf die Typen der Razor-Komponente <c>UebersichtErgebnisse</c> ab.
    /// Die Kennzahlen je Version werden über die Sitzung gepuffert, solange sich ihr Simulationsstand nicht ändert.
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

        /// <summary>Mit dem Ladeweg des Kerns.</summary>
        internal UebersichtErgebnisGaben() : this(EnergievergleichCtrl.Lade) { }

        /// <summary>Mit einem eigenen Ladeweg (Proben).</summary>
        internal UebersichtErgebnisGaben(Func<int, bool, VariantenDaten> lade)
        {
            _lade = lade ?? throw new ArgumentNullException(nameof(lade));
        }

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
            erg.Gruppen = Abbilden(vergleich);
            return erg;
        }

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
                        // Die Vorgabe des Kerns; ein Benutzerzustand überschreibt später allein dieses Feld.
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
