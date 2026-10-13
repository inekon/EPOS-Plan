using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Hinweis der Importquelle zu einer Zone (MA-1b): Name der Zone, DIN-Nummer, Raumtyp und Nutzungsklasse — Stufen 1 und 2
    /// der Profilregel (F6). Der Zonenplan des Imports kennt sie, die Datenbank nicht.
    /// </summary>
    internal sealed record Zonenquellhinweis(string Zone, int? DinNummer, string Raumtyp, string Nutzungsklasse);

    /// <summary>
    /// <b>Die Hülle des Blatts „Modell prüfen“</b> (MA-1b, Entwurf Geführter Modellaufbau 2, E137 F1/F2): baut aus
    /// <see cref="ModellpruefungCtrl"/> den <see cref="ModellpruefungWeg"/> des Blatts — Rechenstatus, fünf Prüfgruppen mit Ampel,
    /// Zählung, Sprungziel und Befundliste mit benanntem Ziel (Zone, Bauteil), Übernehmen und Zurücknehmen je Gruppe, Alle
    /// übernehmen, Nordrichtung bestätigen. Plattformfrei, keine Datenbank in der Oberfläche.
    ///
    /// <para>Die Quellhinweise je Zone (DIN-Nummer, Raumtyp) reicht der Import über <see cref="HinweiseMerken(int, IReadOnlyList{Zonenquellhinweis})"/>
    /// bzw. <see cref="HinweiseMerken(GebaeudeProjektZeile, IReadOnlyList{Zonenquellhinweis})"/> herein; das nächste Öffnen des
    /// Blatts für dieses Gebäude setzt sie mit <see cref="ModellpruefungCtrl.QuelleSetzen"/> und verbraucht sie.</para>
    /// </summary>
    internal sealed class ModellpruefungHuelle
    {
        private static readonly ConcurrentDictionary<int, IReadOnlyList<Zonenquellhinweis>> HINWEISE_JE_GEBAEUDE =
            new ConcurrentDictionary<int, IReadOnlyList<Zonenquellhinweis>>();
        private static readonly ConditionalWeakTable<GebaeudeProjektZeile, IReadOnlyList<Zonenquellhinweis>> HINWEISE_JE_ZEILE =
            new ConditionalWeakTable<GebaeudeProjektZeile, IReadOnlyList<Zonenquellhinweis>>();

        private readonly ModellpruefungCtrl _ctrl;
        private readonly Func<int, string> _nordrichtung;
        private Dictionary<int, string> _zonen = new Dictionary<int, string>();
        private Dictionary<int, string> _bauteile = new Dictionary<int, string>();

        /// <param name="idGebaeude">Das Projektgebäude (<c>Tab_Gebaeude.ID</c>).</param>
        /// <param name="hinweise">Die Quellhinweise je Zone; <c>null</c> = keine.</param>
        /// <param name="nordrichtung">„Nordrichtung bestätigen“: liefert <c>null</c> bei Erfolg, sonst den Grund; <c>null</c> = der
        /// Weg über <see cref="GebaeudeKatalogHuelle.AusrichtungAendern"/> mit der gespeicherten Richtung.</param>
        internal ModellpruefungHuelle(int idGebaeude, IReadOnlyList<Zonenquellhinweis> hinweise = null, Func<int, string> nordrichtung = null)
        {
            _ctrl = new ModellpruefungCtrl(idGebaeude);
            _nordrichtung = nordrichtung ?? NordrichtungBestaetigen;
            NamenLesen();
            if (hinweise == null) return;
            foreach (Zonenquellhinweis h in hinweise)
            {
                if (h == null) continue;
                foreach (KeyValuePair<int, string> z in _zonen)
                    if (string.Equals(z.Value, h.Zone, StringComparison.OrdinalIgnoreCase))
                        _ctrl.QuelleSetzen(z.Key, h.DinNummer, h.Raumtyp ?? "", h.Nutzungsklasse ?? "");
            }
        }

        /// <summary>Der Controller (Prüfhilfe).</summary>
        internal ModellpruefungCtrl Ctrl => _ctrl;

        /// <summary>
        /// Der Weg für das Blatt; <c>null</c> ohne Projektgebäude. Gemerkte Quellhinweise dieses Gebäudes werden verbraucht.
        /// </summary>
        internal static ModellpruefungWeg Weg(int idGebaeude, string name)
        {
            if (idGebaeude <= 0) return null;
            HINWEISE_JE_GEBAEUDE.TryRemove(idGebaeude, out IReadOnlyList<Zonenquellhinweis> hinweise);
            return new ModellpruefungHuelle(idGebaeude, hinweise).AlsWeg(name);
        }

        /// <summary>Wie <see cref="Weg(int, string)"/>, dazu die an der Zeile gemerkten Hinweise (Import vor der Projektkopie).</summary>
        internal static ModellpruefungWeg Weg(GebaeudeProjektZeile zeile, int idGebaeude)
        {
            if (zeile != null && HINWEISE_JE_ZEILE.TryGetValue(zeile, out IReadOnlyList<Zonenquellhinweis> h))
            {
                HINWEISE_JE_ZEILE.Remove(zeile);
                if (idGebaeude > 0) HinweiseMerken(idGebaeude, h);
            }
            return Weg(idGebaeude, zeile?.Name ?? "");
        }

        /// <summary>Merkt die Quellhinweise eines Projektgebäudes bis zum nächsten Öffnen des Blatts.</summary>
        internal static void HinweiseMerken(int idGebaeude, IReadOnlyList<Zonenquellhinweis> hinweise)
        {
            if (idGebaeude <= 0 || hinweise == null || hinweise.Count == 0) return;
            HINWEISE_JE_GEBAEUDE[idGebaeude] = hinweise;
        }

        /// <summary>Merkt die Quellhinweise an einer eben aufgenommenen Zeile (ihre Projektkopie entsteht erst danach).</summary>
        internal static void HinweiseMerken(GebaeudeProjektZeile zeile, IReadOnlyList<Zonenquellhinweis> hinweise)
        {
            if (zeile == null || hinweise == null || hinweise.Count == 0) return;
            HINWEISE_JE_ZEILE.AddOrUpdate(zeile, hinweise);
        }

        /// <summary>
        /// Die Quellhinweise aus dem Zonenplan eines Imports: je Planzone ihr Name und — soweit das Profil aus einer Zuordnung
        /// kommt — DIN-Nummer, Raumtyp bzw. Nutzungsklasse. Leer ohne Plan.
        /// </summary>
        internal static IReadOnlyList<Zonenquellhinweis> HinweiseAus(Zonenplan plan)
        {
            if (plan == null) return Array.Empty<Zonenquellhinweis>();
            var liste = new List<Zonenquellhinweis>();
            foreach (Planzone z in plan.Zonen)
            {
                Profilquelle q = z?.Quelle;
                if (q == null || string.IsNullOrEmpty(z.Name)) continue;
                int? din = null;
                string raumtyp = "", klasse = "";
                if (string.Equals(q.Art, RaumnutzungSchema.ZUORDNUNG_DIN, StringComparison.Ordinal)
                    && int.TryParse(q.Schluessel, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)) din = n;
                else if (string.Equals(q.Art, RaumnutzungSchema.ZUORDNUNG_HOTTCAD, StringComparison.Ordinal)) raumtyp = q.Schluessel ?? "";
                else if (string.Equals(q.Art, RaumnutzungSchema.ZUORDNUNG_IFC, StringComparison.Ordinal)) klasse = q.Schluessel ?? "";
                else continue;
                liste.Add(new Zonenquellhinweis(z.Name, din, raumtyp, klasse));
            }
            return liste;
        }

        /// <summary>Die Delegaten als Weg des Blatts.</summary>
        internal ModellpruefungWeg AlsWeg(string name)
            => new ModellpruefungWeg
            {
                Gebaeudename = name ?? "",
                Pruefen = Pruefen,
                Uebernehmen = g => Ausgang(Sicher(() => _ctrl.Uebernehmen(g))),
                AlleUebernehmen = () => Ausgang(Sicher(_ctrl.AlleUebernehmen)),
                Zuruecknehmen = g => Ausgang(Sicher(() => _ctrl.Zuruecknehmen(g))),
                NordrichtungBestaetigen = () =>
                {
                    string grund = _nordrichtung(_ctrl.IdGebaeude);
                    ModellpruefungStand stand = Pruefen();
                    return grund == null
                        ? new ModellpruefungAusgang(true, MyResource.Resource.MPB_MSG_NORD_BESTAETIGT, stand)
                        : new ModellpruefungAusgang(false, grund, stand);
                }
            };

        /// <summary>Prüft und baut den Stand des Blatts; <c>null</c>, wenn das Gebäude nicht zu lesen ist.</summary>
        internal ModellpruefungStand Pruefen()
        {
            Modellpruefergebnis e;
            try { e = _ctrl.Pruefen(); }
            catch (Exception ex) when (ex is not OutOfMemoryException) { return null; }
            return Stand(e);
        }

        private ModellpruefungStand Stand(Modellpruefergebnis e)
        {
            if (e == null) return null;
            NamenLesen();
            var gruppen = e.Gruppen.OrderBy(g => (int)g.Gruppe).Select(g => new ModellpruefungGruppenzeile(
                g.Gruppe, ModellpruefungCtrl.Gruppenname(g.Gruppe), g.Ampel, g.Anzahl, g.Vorschlaege, g.Sprungziel,
                _ctrl.KannZuruecknehmen(g.Gruppe),
                g.Befunde.Select(b => new ModellpruefungBefundzeile(b.Text ?? "", b.Ampel, Ziel(b), b.Vorschlag?.Text ?? "")).ToList()))
                .ToList();
            return new ModellpruefungStand(e.Status, ModellpruefungCtrl.Statusname(e.Status), gruppen);
        }

        /// <summary>Das benannte Ziel eines Befunds: „Zone — Bauteil“, nur die Zone oder das Gebäude (leer).</summary>
        internal string Ziel(Modellbefund b)
        {
            string zone = b.IdZone is int iz && _zonen.TryGetValue(iz, out string zn) ? zn : "";
            string bauteil = b.IdBauteil is int ib && _bauteile.TryGetValue(ib, out string bn) ? bn : "";
            if (zone.Length > 0 && bauteil.Length > 0) return zone + " — " + bauteil;
            return zone.Length > 0 ? zone : bauteil;
        }

        private ModellpruefungAusgang Ausgang(Uebernahmeergebnis u)
        {
            if (u == null) return new ModellpruefungAusgang(false, MyResource.Resource.MPB_MSG_NICHT_GELESEN, Pruefen());
            string meldung = u.Meldung ?? "";
            if (meldung.Length == 0)
                meldung = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.MPB_MSG_UEBERNOMMEN, u.Uebernommen,
                                        u.Verworfen?.Count ?? 0);
            return new ModellpruefungAusgang(u.Ok, meldung, u.Neu != null ? Stand(u.Neu) : Pruefen());
        }

        private static Uebernahmeergebnis Sicher(Func<Uebernahmeergebnis> f)
        {
            try { return f(); }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return new Uebernahmeergebnis(false, ex.Message, Array.Empty<Pruefgruppe>(), 0, Array.Empty<string>(), false, null);
            }
        }

        /// <summary>Liest die Namen der Zonen und Bauteile des Gebäudes (für das benannte Ziel je Befund).</summary>
        private void NamenLesen()
        {
            try
            {
                List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(_ctrl.IdGebaeude) ?? new List<ZoneModel>();
                _zonen = zonen.GroupBy(z => z.ID).ToDictionary(g => g.Key, g => g.First().Bezeichner ?? "");
                _bauteile = zonen.Where(z => z.Bauteile != null).SelectMany(z => z.Bauteile)
                                 .GroupBy(b => b.ID).ToDictionary(g => g.Key, g => g.First().Bezeichner ?? "");
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                _zonen = new Dictionary<int, string>();
                _bauteile = new Dictionary<int, string>();
            }
        }

        /// <summary>
        /// „Nordrichtung bestätigen“: schreibt die gespeicherte Richtung der Planoberseite unverändert als Eingabe (Herkunft
        /// „Eingabe“ statt „Annahme“) über den Weg „Ausrichtung ändern…“; <c>null</c> = bestätigt, sonst der Grund.
        /// </summary>
        private static string NordrichtungBestaetigen(int idGebaeude)
        {
            EPOS.UI.Dialoge.Import.GebaeudeAusrichtungDaten a = GebaeudeKatalogHuelle.Ausrichtung(idGebaeude);
            if (a == null || !a.Aenderbar) return string.IsNullOrEmpty(a?.Hinweis) ? MyResource.Resource.MPB_HINWEIS_NORD : a.Hinweis;
            EPOS.UI.Dialoge.Import.GebaeudeAusrichtungErgebnis e = GebaeudeKatalogHuelle.AusrichtungAendern(idGebaeude, a.PlanoberseiteGrad);
            return e.Ok ? null : e.Meldung;
        }
    }
}
