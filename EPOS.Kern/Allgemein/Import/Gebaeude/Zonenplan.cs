using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine freie Zone des Zonenplans</b> (Mehrzonenkonzept 6.4): Schlüssel, Name, Nutzung und Herkunft. Welche Räume sie
    /// trägt, hält der <see cref="Zonenplan"/>; ihre Beheizung folgt aus den Räumen.
    /// </summary>
    internal sealed class Planzone
    {
        /// <summary>Der stabile, sprachneutrale Schlüssel <c>Z:&lt;n&gt;</c>; innerhalb eines Plans nie wiederverwendet.</summary>
        internal string Schluessel { get; set; } = "";

        /// <summary>Der Name der Zone (<c>Tab_Zone.Bezeichner</c> beim Speichern); im Plan eindeutig ohne Unterschied der Schreibung.</summary>
        internal string Name { get; set; } = "";

        /// <summary>Die Nutzung (<see cref="Zonenplan.NUTZUNGEN"/>); <c>null</c> = keine.</summary>
        internal string Nutzung { get; set; }

        /// <summary>Der Schlüssel der Regelzone, aus der sie stammt (<see cref="Importzone.Schluessel"/>); <c>null</c> = von Hand angelegt.</summary>
        internal string Herkunft { get; set; }

        /// <summary>Die Kennung der Datei, die die Zone trägt (Zone, Geschoss, Raum, Gebäude); <c>null</c> = aus den Räumen.</summary>
        internal string Quellkennung { get; set; }

        /// <summary>Von Hand angelegt (<see cref="Zonenplan.ZoneAnlegen"/>) — bleibt beim Aufheben der Zonierung stehen.</summary>
        internal bool Angelegt { get; set; }

        /// <summary>Von Hand verändert (Räume hinein oder heraus) — unter der Mindestgröße eine Warnung statt eines Zuschlags.</summary>
        internal bool Geaendert { get; set; }

        internal Planzone Kopie() => (Planzone)MemberwiseClone();

        public override string ToString() => Schluessel + " " + Name + (Nutzung == null ? "" : " [" + Nutzung + "]");
    }

    /// <summary>Ein Schritt am Zonenplan: gelungen oder benannt abgelehnt; mit dem Schlüssel einer neuen Zone.</summary>
    /// <param name="Ok">Gelungen? Abgelehnt heißt: der Plan ist unverändert.</param>
    /// <param name="Meldung">Die Ablehnung (Fehler) oder ein Hinweis zum gelungenen Schritt; <c>null</c> = keiner.</param>
    /// <param name="Schluessel">Der Schlüssel der angelegten Zone; sonst <c>null</c>.</param>
    internal sealed record Planschritt(bool Ok, PruefMeldung Meldung = null, string Schluessel = null);

    /// <summary>Eine Zeile der Flächenbilanz des Plans — auch eine Zone ohne Raum (0 m²).</summary>
    internal sealed record Planbilanzzeile(string Schluessel, string Name, string Nutzung, bool? Beheizt, int Raeume, double FlaecheM2);

    /// <summary>
    /// <b>Der Zonenplan eines importierten Gebäudes</b> (Mehrzonenkonzept 6.4): eine geordnete Liste freier Zonen
    /// (<see cref="Planzone"/>) und die Zuordnung Raum → Zone, ein Raum <b>nicht zugeordnet</b> oder <b>außerhalb</b> (von
    /// der Regel bewusst draußen gelassen, etwa die unbeheizten Räume im Einzonenweg). Er entsteht aus dem Regelvorschlag
    /// (<see cref="Vorschlag"/>; jede Vorschlagszone wird eine freie Zone mit ihrem Namen, unter Z6 mit der Nutzung aus der
    /// Nutzungsklasse) und wird dann nur noch von Hand geändert. Jede Operation ist deterministisch und lehnt benannt ab,
    /// statt still zu wirken; eine abgelehnte Operation lässt den Plan unverändert. Die Zonierung entsteht aus dem Plan
    /// (<see cref="Zonieren"/>); gespeichert wird nur ein Plan ohne nicht zugeordneten Raum (<see cref="Abschlusspruefung"/>).
    /// Ohne Datenbank und ohne Oberfläche.
    /// </summary>
    internal sealed class Zonenplan
    {
        /// <summary>Das Präfix der Zonenschlüssel.</summary>
        internal const string SCHLUESSEL_PRAEFIX = "Z:";

        /// <summary>Die wählbaren Nutzungen einer Zone — die Nutzungen der ausgelieferten Konditionierungsvorlagen; dazu „keine“ (<c>null</c>).</summary>
        internal static readonly IReadOnlyList<string> NUTZUNGEN = new[] { DbWerte.KOND_NUTZUNG_WOHNEN, DbWerte.KOND_NUTZUNG_BUERO, DbWerte.KOND_NUTZUNG_SCHULE };

        /// <summary>Meldung (F): Speichern mit nicht zugeordneten Räumen — Zahl und bis fünf Namen.</summary>
        internal const string ZUORDNUNG_UNVOLLSTAENDIG = "ZUORDNUNG_UNVOLLSTAENDIG";
        /// <summary>Ablehnung: Zonenname leer.</summary>
        internal const string PLAN_NAME_LEER = "PLAN_NAME_LEER";
        /// <summary>Ablehnung: Zonenname schon vergeben.</summary>
        internal const string PLAN_NAME_DOPPELT = "PLAN_NAME_DOPPELT";
        /// <summary>Ablehnung: Nutzung nicht wählbar.</summary>
        internal const string PLAN_NUTZUNG_UNGUELTIG = "PLAN_NUTZUNG_UNGUELTIG";
        /// <summary>Ablehnung: Zone unbekannt.</summary>
        internal const string PLAN_ZONE_UNBEKANNT = "PLAN_ZONE_UNBEKANNT";
        /// <summary>Ablehnung: Raum nicht in diesem Gebäude.</summary>
        internal const string PLAN_RAUM_UNBEKANNT = "PLAN_RAUM_UNBEKANNT";
        /// <summary>Ablehnung: Geschoss ohne Raum in diesem Gebäude.</summary>
        internal const string PLAN_GESCHOSS_UNBEKANNT = "PLAN_GESCHOSS_UNBEKANNT";
        /// <summary>Ablehnung: Raum und Zielzone (bzw. die gewählten Räume untereinander) nicht gleich beheizt.</summary>
        internal const string PLAN_BEHEIZUNG = "PLAN_BEHEIZUNG";
        /// <summary>Hinweis (W): Räume eines Geschosses anderer Beheizung blieben, wo sie waren.</summary>
        internal const string PLAN_BEHEIZUNG_TEIL = "PLAN_BEHEIZUNG_TEIL";
        /// <summary>Ablehnung: mehr Zonen als die Pflegegrenze.</summary>
        internal const string PLAN_ZU_VIELE_ZONEN = "PLAN_ZU_VIELE_ZONEN";
        /// <summary>Ablehnung: die Regel trägt das Gebäude nicht.</summary>
        internal const string PLAN_REGEL_UNGUELTIG = "PLAN_REGEL_UNGUELTIG";
        /// <summary>Hinweis (I): Räume, die die Regel außerhalb der Zonen lässt.</summary>
        internal const string PLAN_REST_AUSSERHALB = "PLAN_REST_AUSSERHALB";

        private readonly GebaeudeAbbild _abbild;
        private readonly int _index;
        private readonly Dictionary<string, bool> _haken;
        private readonly string _praefix;
        private readonly List<Planzone> _zonen = new List<Planzone>();
        private readonly Dictionary<string, string> _zoneJeRaum = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> _ausserhalb = new HashSet<string>(StringComparer.Ordinal);
        private int _naechste = 1;

        private Zonenplan(GebaeudeAbbild abbild, int index, IReadOnlyDictionary<string, bool> haken)
        {
            _abbild = abbild;
            _index = index;
            _haken = haken == null ? new Dictionary<string, bool>(StringComparer.Ordinal)
                                   : new Dictionary<string, bool>(haken.ToDictionary(p => p.Key, p => p.Value), StringComparer.Ordinal);
            _praefix = string.Equals(abbild?.Format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal)
                ? IfcImportProfil.MELDUNGSPRAEFIX : GbxmlImportProfil.MELDUNGSPRAEFIX;
        }

        /// <summary>Die Regel des Startvorschlags (bzw. der letzten Neubildung).</summary>
        internal string Regel { get; private set; } = "";

        /// <summary>Die Ablehnung des Regelvorschlags, aus dem der Plan entstand; <c>null</c> = keine.</summary>
        internal PruefMeldung Ablehnung { get; private set; }

        /// <summary>Das Gebäude der Datei.</summary>
        internal AbbildGebaeude Gebaeude => _abbild.Gebaeude[_index];

        /// <summary>Die Zonen in Planreihenfolge.</summary>
        internal IReadOnlyList<Planzone> Zonen => _zonen;

        /// <summary>Die Haken der Raumliste, mit denen der Plan gebildet ist; <c>null</c> = wie gelesen.</summary>
        internal IReadOnlyDictionary<string, bool> Haken => _haken;

        /// <summary>Der Schlüssel der Zone eines Raums; <c>null</c> = keine (nicht zugeordnet oder außerhalb).</summary>
        internal string ZoneVon(string raum) => raum != null && _zoneJeRaum.TryGetValue(raum, out string z) ? z : null;

        /// <summary>Liegt ein Raum nach der Regel bewusst außerhalb der Zonen?</summary>
        internal bool Ausserhalb(string raum) => raum != null && _ausserhalb.Contains(raum);

        /// <summary>Die Räume einer Zone in Dateireihenfolge; leer für eine unbekannte Zone.</summary>
        internal IReadOnlyList<AbbildRaum> RaeumeVon(string schluessel)
            => Gebaeude.Raeume.Where(r => string.Equals(ZoneVon(r.Kennung), schluessel, StringComparison.Ordinal)).ToList();

        /// <summary>Die Räume in keiner Zone und nicht außerhalb, in Dateireihenfolge — sie sperren das Speichern.</summary>
        internal IReadOnlyList<AbbildRaum> NichtZugeordnet
            => Gebaeude.Raeume.Where(r => ZoneVon(r.Kennung) == null && !_ausserhalb.Contains(r.Kennung)).ToList();

        /// <summary>Die Räume, die die Regel außerhalb der Zonen lässt, in Dateireihenfolge.</summary>
        internal IReadOnlyList<AbbildRaum> RaeumeAusserhalb => Gebaeude.Raeume.Where(r => _ausserhalb.Contains(r.Kennung)).ToList();

        /// <summary>Die Geschosskennungen mit Räumen, in Dateireihenfolge.</summary>
        internal IReadOnlyList<string> Geschosse
            => Gebaeude.Raeume.Select(r => r.GeschossKennung).Where(g => g != null).Distinct(StringComparer.Ordinal).ToList();

        /// <summary>Die Zone eines Schlüssels; <c>null</c> = keine.</summary>
        internal Planzone Zone(string schluessel) => _zonen.FirstOrDefault(z => string.Equals(z.Schluessel, schluessel, StringComparison.Ordinal));

        /// <summary>Ist ein Raum wirksam beheizt (mit den Haken des Plans)?</summary>
        internal bool Beheizt(AbbildRaum r) => GebaeudeRaumzeile.BeheiztWirksam(r, _haken);

        /// <summary>Die Beheizung einer Zone aus ihren Räumen; <c>null</c> ohne Raum.</summary>
        internal bool? ZoneBeheizt(string schluessel)
        {
            IReadOnlyList<AbbildRaum> raeume = RaeumeVon(schluessel);
            return raeume.Count == 0 ? (bool?)null : raeume.Any(Beheizt);
        }

        /// <summary>Der Anzeigename eines Raums: sein Name, sonst seine Kennung.</summary>
        internal static string Raumname(AbbildRaum r) => r == null ? "" : string.IsNullOrWhiteSpace(r.Name) ? r.Kennung : r.Name.Trim();

        // ==================================================================
        //  Entstehen
        // ==================================================================

        /// <summary>
        /// <b>Der Plan aus dem Regelvorschlag</b>: die Zonierung nach <paramref name="regel"/> (<c>null</c> = Vorgabe der Datei)
        /// mit den Haken und den Umhängungen (der Eingang der gespeicherten Zuordnungen von Hand, in ihrer Reihenfolge), jede
        /// Zone eine freie Zone mit ihrem Namen — unter Z6 mit der Nutzung aus der Nutzungsklasse (<see cref="NutzungAusKlasse"/>),
        /// sonst ohne. Räume, die der Vorschlag außerhalb lässt, liegen außerhalb. <c>null</c> ohne Gebäude.
        /// </summary>
        internal static Zonenplan Vorschlag(GebaeudeAbbild abbild, int index, string regel = null,
                                            IReadOnlyDictionary<string, bool> haken = null,
                                            IReadOnlyList<Raumumhaengung> umhaengungen = null)
        {
            if (abbild == null || index < 0 || index >= abbild.Gebaeude.Count) return null;
            var plan = new Zonenplan(abbild, index, haken);
            plan.AusRegelBilden(regel, umhaengungen);
            return plan;
        }

        /// <summary>
        /// <b>Der gespeicherte Plan</b> eines früheren Imports derselben Datei: die Zonen in der gespeicherten Reihenfolge mit
        /// Namen und Nutzung, die Räume über ihre Paarung (Raumkennung → Zonen-Id). Ein Raum ohne Paarung liegt außerhalb,
        /// wenn die gespeicherte Regel ihn draußen lässt, sonst ist er nicht zugeordnet; eine Paarung auf eine unbekannte
        /// Zone oder einen fremden Raum bleibt unbeachtet. <c>null</c> ohne Gebäude.
        /// </summary>
        internal static Zonenplan AusGespeichert(GebaeudeAbbild abbild, int index, string regel, IReadOnlyDictionary<string, bool> haken,
                                                 IReadOnlyList<(int Id, string Name, string Nutzung)> zonen,
                                                 IReadOnlyList<(string Raum, int IdZone)> paarungen)
        {
            if (abbild == null || index < 0 || index >= abbild.Gebaeude.Count) return null;
            var plan = new Zonenplan(abbild, index, haken);
            (IReadOnlyList<string> regeln, string vorgabe, bool _) = GebaeudeZonierung.Waehlbar(abbild, index);
            plan.Regel = regel != null && regeln.Contains(regel) ? regel : vorgabe;
            var jeId = new Dictionary<int, string>();
            foreach ((int id, string name, string nutzung) in zonen ?? Array.Empty<(int, string, string)>())
            {
                if (jeId.ContainsKey(id)) continue;
                Planzone z = plan.Neu(plan.FreierName(name), NUTZUNGEN.Contains(nutzung) ? nutzung : null, null, null, angelegt: false);
                jeId[id] = z.Schluessel;
            }
            var eigene = new HashSet<string>(plan.Gebaeude.Raeume.Select(r => r.Kennung), StringComparer.Ordinal);
            foreach ((string raum, int idZone) in paarungen ?? Array.Empty<(string, int)>())
                if (raum != null && eigene.Contains(raum) && jeId.TryGetValue(idZone, out string s) && !plan._zoneJeRaum.ContainsKey(raum))
                    plan._zoneJeRaum[raum] = s;
            GebaeudeZonierung vorschlag = GebaeudeZonierung.Bilden(abbild, index, plan.Regel, haken);
            foreach (AbbildRaum r in plan.Gebaeude.Raeume)
                if (!plan._zoneJeRaum.ContainsKey(r.Kennung) && !vorschlag.Abgelehnt && vorschlag.ZoneVon(r.Kennung) < 0)
                    plan._ausserhalb.Add(r.Kennung);
            return plan;
        }

        /// <summary>Eine tiefe Kopie — für Rückgängig im Dialog.</summary>
        internal Zonenplan Kopie()
        {
            var k = new Zonenplan(_abbild, _index, _haken) { Regel = Regel, Ablehnung = Ablehnung, _naechste = _naechste };
            k._zonen.AddRange(_zonen.Select(z => z.Kopie()));
            foreach (KeyValuePair<string, string> p in _zoneJeRaum) k._zoneJeRaum[p.Key] = p.Value;
            k._ausserhalb.UnionWith(_ausserhalb);
            return k;
        }

        private void AusRegelBilden(string regel, IReadOnlyList<Raumumhaengung> umhaengungen)
        {
            _zonen.Clear();
            _zoneJeRaum.Clear();
            _ausserhalb.Clear();
            GebaeudeZonierung zon = GebaeudeZonierung.Bilden(_abbild, _index, regel, _haken, umhaengungen);
            Regel = zon.Regel;
            Ablehnung = zon.Meldungen.FirstOrDefault(m => m.Stufe == PruefStufe.Fehler);
            if (zon.Abgelehnt) return;
            foreach (Importzone iz in zon.Zonen)
            {
                Planzone z = Neu(FreierName(iz.Name), NutzungAus(zon.Regel, iz), iz.Schluessel, iz.Quellkennung,
                                 angelegt: iz.Schluessel.StartsWith(GebaeudeZonierung.HAND_PRAEFIX, StringComparison.Ordinal));
                z.Geaendert = iz.Handgeaendert;
                foreach (AbbildRaum r in iz.Raeume) _zoneJeRaum[r.Kennung] = z.Schluessel;
            }
            foreach (AbbildRaum r in Gebaeude.Raeume)
                if (!_zoneJeRaum.ContainsKey(r.Kennung)) _ausserhalb.Add(r.Kennung);
        }

        private Planzone Neu(string name, string nutzung, string herkunft, string quellkennung, bool angelegt)
        {
            var z = new Planzone
            {
                Schluessel = SCHLUESSEL_PRAEFIX + (_naechste++).ToString(CultureInfo.InvariantCulture),
                Name = name, Nutzung = nutzung, Herkunft = herkunft, Quellkennung = quellkennung, Angelegt = angelegt,
            };
            _zonen.Add(z);
            return z;
        }

        /// <summary>Ein im Plan freier Name: der Wunsch, sonst mit „ 2“, „ 3“ …</summary>
        private string FreierName(string wunsch)
        {
            string basis = string.IsNullOrWhiteSpace(wunsch) ? GebaeudeZonenuebernahme.ZONE_BEZEICHNUNG : wunsch.Trim();
            string name = basis;
            for (int n = 2; NameVergeben(name, null); n++) name = basis + " " + n.ToString(CultureInfo.InvariantCulture);
            return name;
        }

        private bool NameVergeben(string name, string ausser)
            => _zonen.Any(z => !string.Equals(z.Schluessel, ausser, StringComparison.Ordinal)
                               && string.Equals(z.Name, name, StringComparison.OrdinalIgnoreCase));

        // ==================================================================
        //  Nutzung aus der Regel
        // ==================================================================

        /// <summary>
        /// Die Nutzung einer Nutzungsklasse (Z6): Büro → <c>BUERO</c>; Wohnen, Schlafen, Küche → <c>WOHNEN</c>; Sanitär,
        /// Verkehr, Lager, Technik, Sport, Gastronomie und Sonstige allein ergeben keine (<c>null</c>).
        /// </summary>
        internal static string NutzungAusKlasse(string klasse)
        {
            switch (klasse)
            {
                case "Buero": return DbWerte.KOND_NUTZUNG_BUERO;
                case "Wohnen":
                case "Schlafen":
                case "Kueche": return DbWerte.KOND_NUTZUNG_WOHNEN;
                default: return null;
            }
        }

        /// <summary>
        /// Die Nutzung einer Vorschlagszone: unter Z6 die der Nutzungsklasse mit der größten Fläche (Gleichstand: mehr Räume,
        /// dann Klasse ordinal — dieselbe Ordnung wie der Zonenname), unter jeder anderen Regel keine. Vorbelegt wird nur eine
        /// beheizte Zone; eine unbeheizte bekommt keine Nutzung.
        /// </summary>
        internal static string NutzungAus(string regel, Importzone zone)
        {
            if (zone == null || !zone.IstBeheizt || !string.Equals(regel, IfcImportProfil.ZONENREGEL_Z6, StringComparison.Ordinal)
                || zone.Raeume.Count == 0)
                return null;
            string klasse = zone.Raeume.GroupBy(GebaeudeZonierung.Nutzungsklasse)
                .Select(gr => (Klasse: gr.Key, A: gr.Sum(r => r.FlaecheM2 > 0.0 ? r.FlaecheM2.Value : 0.0), N: gr.Count()))
                .OrderByDescending(k => k.A).ThenByDescending(k => k.N).ThenBy(k => k.Klasse, StringComparer.Ordinal)
                .First().Klasse;
            return NutzungAusKlasse(klasse);
        }

        // ==================================================================
        //  Operationen
        // ==================================================================

        /// <summary><b>Legt eine Zone an</b> (am Ende, ohne Raum); der Schlüssel steht im Schritt.</summary>
        internal Planschritt ZoneAnlegen(string name, string nutzung)
        {
            string n = (name ?? "").Trim();
            if (n.Length == 0) return Ab(PLAN_NAME_LEER);
            if (NameVergeben(n, null)) return Ab(PLAN_NAME_DOPPELT, n);
            if (nutzung != null && !NUTZUNGEN.Contains(nutzung)) return Ab(PLAN_NUTZUNG_UNGUELTIG, nutzung);
            if (_zonen.Count >= GebaeudeZonenregeln.PFLEGEGRENZE)
                return Ab(PLAN_ZU_VIELE_ZONEN, GebaeudeZonenregeln.PFLEGEGRENZE.ToString(CultureInfo.InvariantCulture));
            Planzone z = Neu(n, nutzung, null, null, angelegt: true);
            return new Planschritt(true, null, z.Schluessel);
        }

        /// <summary><b>Löscht eine Zone</b>; ihre Räume sind danach nicht zugeordnet.</summary>
        internal Planschritt ZoneLoeschen(string schluessel)
        {
            Planzone z = Zone(schluessel);
            if (z == null) return Ab(PLAN_ZONE_UNBEKANNT, schluessel ?? "");
            foreach (string raum in _zoneJeRaum.Where(p => p.Value == z.Schluessel).Select(p => p.Key).ToList()) _zoneJeRaum.Remove(raum);
            _zonen.Remove(z);
            return new Planschritt(true);
        }

        /// <summary><b>Benennt eine Zone um</b> — nicht leer, im Plan eindeutig.</summary>
        internal Planschritt ZoneUmbenennen(string schluessel, string name)
        {
            Planzone z = Zone(schluessel);
            if (z == null) return Ab(PLAN_ZONE_UNBEKANNT, schluessel ?? "");
            string n = (name ?? "").Trim();
            if (n.Length == 0) return Ab(PLAN_NAME_LEER);
            if (NameVergeben(n, z.Schluessel)) return Ab(PLAN_NAME_DOPPELT, n);
            z.Name = n;
            return new Planschritt(true);
        }

        /// <summary><b>Setzt die Nutzung einer Zone</b>: eine der <see cref="NUTZUNGEN"/> oder <c>null</c> (keine).</summary>
        internal Planschritt NutzungSetzen(string schluessel, string nutzung)
        {
            Planzone z = Zone(schluessel);
            if (z == null) return Ab(PLAN_ZONE_UNBEKANNT, schluessel ?? "");
            if (nutzung != null && !NUTZUNGEN.Contains(nutzung)) return Ab(PLAN_NUTZUNG_UNGUELTIG, nutzung);
            z.Nutzung = nutzung;
            return new Planschritt(true);
        }

        /// <summary>
        /// <b>Stellt den Haken „beheizt“ eines Raums um</b> (der Ausweg bei ungleicher Beheizung): Der Raum bleibt in seiner
        /// Zone; ist sie danach gemischt beheizt, warnt die Zonierung. Ein unbekannter Raum lehnt benannt ab.
        /// </summary>
        internal Planschritt BeheizungSetzen(string raum, bool beheizt)
        {
            if (raum == null || !Gebaeude.Raeume.Any(r => string.Equals(r.Kennung, raum, StringComparison.Ordinal)))
                return Ab(PLAN_RAUM_UNBEKANNT, raum ?? "");
            _haken[raum] = beheizt;
            return new Planschritt(true);
        }

        /// <summary>
        /// <b>Hebt die Zonierung auf</b>: Jeder Raum ist danach nicht zugeordnet (auch die bisher außerhalb); die Zonen aus dem
        /// Regelvorschlag entfallen, die von Hand angelegten bleiben leer stehen.
        /// </summary>
        internal Planschritt ZonierungAufheben()
        {
            _zoneJeRaum.Clear();
            _ausserhalb.Clear();
            _zonen.RemoveAll(z => !z.Angelegt);
            return new Planschritt(true);
        }

        /// <summary>
        /// <b>Ordnet Räume einer Zone zu</b> — alle oder keinen: ein unbekannter Raum, eine unbekannte Zone oder ein Raum
        /// anderer Beheizung als die Zone (bzw. als der erste gewählte, wenn die Zone leer ist) lehnt benannt ab; der Ausweg
        /// ist der Haken „beheizt“ des Raums. <paramref name="zielzone"/> <c>null</c> = die Räume aus ihrer Zone nehmen
        /// (danach nicht zugeordnet).
        /// </summary>
        internal Planschritt Zuordnen(IEnumerable<string> raeume, string zielzone)
        {
            List<AbbildRaum> wahl = new List<AbbildRaum>();
            foreach (string k in (raeume ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal))
            {
                AbbildRaum r = Gebaeude.Raeume.FirstOrDefault(x => string.Equals(x.Kennung, k, StringComparison.Ordinal));
                if (r == null) return Ab(PLAN_RAUM_UNBEKANNT, k ?? "");
                wahl.Add(r);
            }
            if (wahl.Count == 0) return new Planschritt(true);
            if (zielzone == null)
            {
                foreach (AbbildRaum r in wahl) Herausnehmen(r);
                return new Planschritt(true);
            }
            Planzone ziel = Zone(zielzone);
            if (ziel == null) return Ab(PLAN_ZONE_UNBEKANNT, zielzone);
            var wahlKennungen = new HashSet<string>(wahl.Select(r => r.Kennung), StringComparer.Ordinal);
            List<AbbildRaum> bleibende = RaeumeVon(ziel.Schluessel).Where(r => !wahlKennungen.Contains(r.Kennung)).ToList();
            bool warm = bleibende.Count > 0 ? bleibende.Any(Beheizt) : Beheizt(wahl[0]);
            AbbildRaum anders = wahl.FirstOrDefault(r => Beheizt(r) != warm);
            if (anders != null) return Ab(PLAN_BEHEIZUNG, Raumname(anders), ziel.Name);
            foreach (AbbildRaum r in wahl) Hinein(r, ziel);
            return new Planschritt(true);
        }

        /// <summary>
        /// <b>Ordnet die Räume eines Geschosses einer Zone zu</b>: die gleich beheizten gehen hinein, die anderen bleiben, wo
        /// sie sind — benannt (Warnung mit Zahl und Namen); passt keiner, ist der Schritt abgelehnt.
        /// </summary>
        internal Planschritt GeschossZuordnen(string geschoss, string zielzone)
        {
            List<AbbildRaum> raeume = Gebaeude.Raeume.Where(r => string.Equals(r.GeschossKennung, geschoss, StringComparison.Ordinal)).ToList();
            if (geschoss == null || raeume.Count == 0) return Ab(PLAN_GESCHOSS_UNBEKANNT, geschoss ?? "");
            Planzone ziel = Zone(zielzone);
            if (ziel == null) return Ab(PLAN_ZONE_UNBEKANNT, zielzone ?? "");
            var geschossKennungen = new HashSet<string>(raeume.Select(r => r.Kennung), StringComparer.Ordinal);
            List<AbbildRaum> bleibende = RaeumeVon(ziel.Schluessel).Where(r => !geschossKennungen.Contains(r.Kennung)).ToList();
            bool warm;
            if (bleibende.Count > 0) warm = bleibende.Any(Beheizt);
            else
            {
                // Eine leere Zone nimmt die Beheizung der größeren Fläche des Geschosses (Gleichstand: beheizt).
                double a = raeume.Where(Beheizt).Sum(r => r.FlaecheM2 ?? 0.0), b = raeume.Where(r => !Beheizt(r)).Sum(r => r.FlaecheM2 ?? 0.0);
                warm = raeume.Any(Beheizt) && a >= b;
            }
            List<AbbildRaum> passend = raeume.Where(r => Beheizt(r) == warm).ToList();
            List<AbbildRaum> anders = raeume.Where(r => Beheizt(r) != warm).ToList();
            if (passend.Count == 0) return Ab(PLAN_BEHEIZUNG, Raumname(anders[0]), ziel.Name);
            foreach (AbbildRaum r in passend) Hinein(r, ziel);
            return anders.Count == 0 ? new Planschritt(true)
                : new Planschritt(true, Hinweis(PruefStufe.Warnung, PLAN_BEHEIZUNG_TEIL,
                                                anders.Count.ToString(CultureInfo.InvariantCulture), Liste(anders), ziel.Name));
        }

        /// <summary>
        /// <b>Ordnet die nicht zugeordneten Räume nach einer Regel zu</b>: je Raum seine Zone im Vorschlag der Regel — in die
        /// Planzone, die aus derselben Regelzone stammt, sonst in die gleichnamige gleicher Beheizung, sonst in eine neue
        /// (Name und Nutzung der Regelzone). Was die Regel außerhalb lässt, liegt danach außerhalb (Hinweis). Die übrigen
        /// Räume bleiben, wo sie sind.
        /// </summary>
        internal Planschritt RestNachRegelZuordnen(string regel)
        {
            (IReadOnlyList<string> regeln, string _, bool _) = GebaeudeZonierung.Waehlbar(_abbild, _index);
            if (regel == null || !regeln.Contains(regel)) return Ab(PLAN_REGEL_UNGUELTIG, regel ?? "", string.Join(", ", regeln));
            IReadOnlyList<AbbildRaum> rest = NichtZugeordnet;
            if (rest.Count == 0) return new Planschritt(true);
            GebaeudeZonierung zon = GebaeudeZonierung.Bilden(_abbild, _index, regel, _haken);
            var draussen = new List<AbbildRaum>();
            foreach (AbbildRaum r in rest)
            {
                int i = zon.ZoneVon(r.Kennung);
                if (i < 0)
                {
                    _ausserhalb.Add(r.Kennung);
                    draussen.Add(r);
                    continue;
                }
                Importzone iz = zon.Zonen[i];
                bool warm = Beheizt(r);
                Planzone ziel = _zonen.FirstOrDefault(z => z.Herkunft == iz.Schluessel && Passt(z, warm))
                                ?? _zonen.FirstOrDefault(z => string.Equals(z.Name, iz.Name, StringComparison.OrdinalIgnoreCase) && Passt(z, warm))
                                ?? Neu(FreierName(iz.Name), NutzungAus(regel, iz), iz.Schluessel, iz.Quellkennung, angelegt: false);
                _zoneJeRaum[r.Kennung] = ziel.Schluessel;
            }
            return draussen.Count == 0 ? new Planschritt(true)
                : new Planschritt(true, Hinweis(PruefStufe.Info, PLAN_REST_AUSSERHALB,
                                                draussen.Count.ToString(CultureInfo.InvariantCulture), Liste(draussen), regel));
        }

        /// <summary>
        /// <b>Bildet den Plan nach einer Regel neu</b> (der Regelwechsel des Dialogs, dort mit Rückfrage): alle Handänderungen
        /// gehen verloren; die Schlüssel zählen weiter.
        /// </summary>
        internal Planschritt NachRegelNeuBilden(string regel)
        {
            (IReadOnlyList<string> regeln, string _, bool _) = GebaeudeZonierung.Waehlbar(_abbild, _index);
            if (regel == null || !regeln.Contains(regel)) return Ab(PLAN_REGEL_UNGUELTIG, regel ?? "", string.Join(", ", regeln));
            AusRegelBilden(regel, null);
            return new Planschritt(true);
        }

        private bool Passt(Planzone z, bool warm)
        {
            bool? b = ZoneBeheizt(z.Schluessel);
            return !b.HasValue || b.Value == warm;
        }

        private void Herausnehmen(AbbildRaum r)
        {
            _ausserhalb.Remove(r.Kennung);
            if (_zoneJeRaum.TryGetValue(r.Kennung, out string alt))
            {
                _zoneJeRaum.Remove(r.Kennung);
                Planzone z = Zone(alt);
                if (z != null) z.Geaendert = true;
            }
        }

        private void Hinein(AbbildRaum r, Planzone ziel)
        {
            if (string.Equals(ZoneVon(r.Kennung), ziel.Schluessel, StringComparison.Ordinal)) return;
            Herausnehmen(r);
            _zoneJeRaum[r.Kennung] = ziel.Schluessel;
            ziel.Geaendert = true;
        }

        // ==================================================================
        //  Ergebnis
        // ==================================================================

        /// <summary><b>Die Zonierung aus dem Plan</b> (<see cref="GebaeudeZonierung.Bilden"/> mit Plan); schreibt nichts.</summary>
        internal GebaeudeZonierung Zonieren() => GebaeudeZonierung.Bilden(_abbild, _index, Regel, _haken, null, this);

        /// <summary>
        /// <b>Die Prüfung am OK</b>: ein Plan mit nicht zugeordneten Räumen wird nicht gespeichert — benannt (Fehler, Zahl und
        /// bis fünf Namen). <c>null</c> = speicherbar.
        /// </summary>
        internal PruefMeldung Abschlusspruefung()
        {
            IReadOnlyList<AbbildRaum> rest = NichtZugeordnet;
            return rest.Count == 0 ? null
                : Hinweis(PruefStufe.Fehler, ZUORDNUNG_UNVOLLSTAENDIG, rest.Count.ToString(CultureInfo.InvariantCulture), Liste(rest));
        }

        /// <summary>
        /// <b>Die Flächenbilanz</b> in Planreihenfolge: je Zone Nutzung, Beheizung (<c>null</c> ohne Raum), Raumzahl und
        /// Fläche — eine Zone ohne Raum mit 0 m². Nicht zugeordnete und außerhalb liegende Räume zählen in keiner Zone.
        /// </summary>
        internal IReadOnlyList<Planbilanzzeile> Bilanz()
            => _zonen.Select(z =>
            {
                IReadOnlyList<AbbildRaum> raeume = RaeumeVon(z.Schluessel);
                return new Planbilanzzeile(z.Schluessel, z.Name, z.Nutzung, ZoneBeheizt(z.Schluessel), raeume.Count,
                                           raeume.Where(r => r.FlaecheM2 > 0.0).Sum(r => r.FlaecheM2.Value));
            }).ToList();

        private Planschritt Ab(string name, params string[] werte) => new Planschritt(false, Hinweis(PruefStufe.Fehler, name, werte));

        private PruefMeldung Hinweis(PruefStufe stufe, string name, params string[] werte) => new PruefMeldung(stufe, _praefix + name, werte);

        private static string Liste(IEnumerable<AbbildRaum> raeume)
        {
            List<string> namen = raeume.Select(Raumname).Distinct(StringComparer.Ordinal).ToList();
            return namen.Count <= 5 ? string.Join(", ", namen) : string.Join(", ", namen.Take(5)) + ", …";
        }

        public override string ToString()
            => Regel + ": " + string.Join("; ", _zonen.Select(z => z + " (" + RaeumeVon(z.Schluessel).Count.ToString(CultureInfo.InvariantCulture) + ")"))
               + " | nicht zugeordnet " + NichtZugeordnet.Count.ToString(CultureInfo.InvariantCulture);
    }
}
