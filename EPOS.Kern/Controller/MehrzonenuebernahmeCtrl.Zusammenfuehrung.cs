using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Der Stand eines Elements im Wiederimport (MA-6): was die Zusammenführung mit ihm tut.</summary>
    public enum Wiederimportstand
    {
        /// <summary>Die Datei bringt das Element neu mit; es wird angelegt.</summary>
        Neu,
        /// <summary>Über die Quellkennung wiedergefunden; Felder der Datei ändern sich.</summary>
        Geaendert,
        /// <summary>Über die Quellkennung wiedergefunden; die Felder der Datei sind gleich.</summary>
        Unveraendert,
        /// <summary>Stammt aus der Datei, fehlt in der neuen Fassung; entfernt nur mit Zustimmung.</summary>
        Entfaellt,
        /// <summary>Von Hand angelegt (ohne Quellkennung); bleibt unberührt.</summary>
        Bleibt
    }

    /// <summary>
    /// Ein Element der Vorschau des Wiederimports (MA-6): Zone oder Bauteil, die Zone, zu der es gehört, sein Name, sein
    /// Stand, die Felder der Datei, die sich ändern, und die Ergänzungen von Hand, die bleiben (Anzeigenamen).
    /// </summary>
    internal sealed record Wiederimportelement(bool IstZone, string Zone, string Bezeichner, Wiederimportstand Stand,
                                               IReadOnlyList<string> Felder, IReadOnlyList<string> Hand);

    /// <summary>Die Vorschau des Wiederimports: Ok, Meldung (leer bei Erfolg) und die Elemente in Anzeigereihenfolge.</summary>
    internal sealed record Wiederimportvorschau(bool Ok, string Meldung, IReadOnlyList<Wiederimportelement> Elemente)
    {
        /// <summary>Die Zahl der Elemente eines Stands, nur Zonen bzw. nur Bauteile.</summary>
        internal int Anzahl(Wiederimportstand stand, bool zonen)
            => Elemente.Count(e => e.Stand == stand && e.IstZone == zonen);
    }

    internal static partial class MehrzonenuebernahmeCtrl
    {
        // =================================================================
        //  MA-6: Wiederimport bewahrt — Zusammenführung über Quellkennung und Herkunft
        // =================================================================

        /// <summary>Relativer Rand des Feldvergleichs der Vorschau.</summary>
        private const double RAND = 1e-9;

        /// <summary>Der Bestand des Gebäudes vor dem Wiederimport: Zonen samt Bauteilen und die Kennungen der Datei je Satz.</summary>
        private sealed class Bestand
        {
            internal List<ZoneModel> Zonen = new List<ZoneModel>();
            internal Dictionary<int, HashSet<string>> ZonenKennungen = new Dictionary<int, HashSet<string>>();
            internal Dictionary<int, HashSet<string>> BauteilKennungen = new Dictionary<int, HashSet<string>>();
            internal HashSet<int> DateiAufbauten = new HashSet<int>();

            internal bool AusDatei(ZoneModel z) => ZonenKennungen.TryGetValue(z.ID, out HashSet<string> k) && k.Count > 0;
            internal bool AusDatei(BauteilModel b) => BauteilKennungen.TryGetValue(b.ID, out HashSet<string> k) && k.Count > 0;
        }

        /// <summary>
        /// Die Paarung von neuem Vorschlag und Bestand: je Zone des Vorschlags die wiedergefundene Zone (oder keine), je
        /// Bauteil des Vorschlags (Zone, Stelle) das wiedergefundene Bauteil; dazu, was entfällt und was von Hand bleibt.
        /// </summary>
        private sealed class Paarung
        {
            internal ZoneModel[] ZoneAlt;
            internal BauteilModel[][] BauteilAlt;
            internal List<ZoneModel> ZonenEntfallend = new List<ZoneModel>();
            internal List<BauteilModel> BauteileEntfallend = new List<BauteilModel>();
            internal List<ZoneModel> ZonenHand = new List<ZoneModel>();
            internal List<BauteilModel> BauteileHand = new List<BauteilModel>();
        }

        private static Bestand BestandLesen(int idGebaeude)
        {
            var b = new Bestand { Zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(idGebaeude) };
            foreach (ZoneModel z in b.Zonen)
            {
                b.ZonenKennungen[z.ID] = Kennungen(z.Quellkennung);
                foreach (BauteilModel t in z.Bauteile) b.BauteilKennungen[t.ID] = Kennungen(t.Quellkennung);
            }
            if (!ImportzuordnungSchema.Vollstaendig()) return b;
            DataTable t2 = DataRepository.GetDataTable(
                "SELECT z.\"Quellkennung\", z.\"ID_Zone\", z.\"ID_Bauteil\", z.\"ID_Aufbau\" FROM \"" + ImportzuordnungSchema.TAB_ZUORDNUNG +
                "\" z INNER JOIN \"" + ImportzuordnungSchema.TAB_QUELLE + "\" q ON q.\"ID\" = z.\"ID_Importquelle\" WHERE q.\"ID_Gebaeude\" = ?",
                new DbParam("@g", idGebaeude));
            foreach (DataRow r in t2.Rows)
            {
                string k = Convert.ToString(r[0], CultureInfo.InvariantCulture) ?? "";
                if (r[1] is not DBNull && b.ZonenKennungen.TryGetValue(Convert.ToInt32(r[1], CultureInfo.InvariantCulture), out HashSet<string> zk)) zk.Add(k);
                if (r[2] is not DBNull && b.BauteilKennungen.TryGetValue(Convert.ToInt32(r[2], CultureInfo.InvariantCulture), out HashSet<string> bk)) bk.Add(k);
                if (r[3] is not DBNull) b.DateiAufbauten.Add(Convert.ToInt32(r[3], CultureInfo.InvariantCulture));
            }
            return b;
        }

        private static HashSet<string> Kennungen(string quellkennung)
        {
            var k = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(quellkennung)) k.Add(Quellkennung.Kuerzen(quellkennung));
            return k;
        }

        /// <summary>Die Kennungen der Datei je Zone des Vorschlags: ihre Räume (Paarung Raum → Zone) und die Quellkennung der Zone.</summary>
        private static List<HashSet<string>> ZonenKennungenNeu(GebaeudeBauteilvorschlag v)
        {
            var liste = v.Zonen.Select(z => Kennungen(z.Quellkennung)).ToList();
            foreach (GebaeudeQuellzuordnung r in v.Raeume)
            {
                int i = 0;
                if (r.ZielId.HasValue)
                {
                    i = -1;
                    for (int j = 0; j < v.Zonen.Count; j++) if (v.Zonen[j].ID == r.ZielId.Value) { i = j; break; }
                }
                if (i >= 0 && i < liste.Count && r.Quellkennung.Length > 0) liste[i].Add(r.Quellkennung);
            }
            return liste;
        }

        /// <summary>Die Kennungen der Datei je Bauteil des Vorschlags (Zone, Stelle) — Zeile des Vorschlags und Quellkennung.</summary>
        private static List<List<HashSet<string>>> BauteilKennungenNeu(GebaeudeBauteilvorschlag v)
        {
            var liste = v.Zonen.Select(z => z.Bauteile.Select(b => Kennungen(b.Quellkennung)).ToList()).ToList();
            var stelle = new int[v.Zonen.Count];
            foreach (GebaeudeBauteilzeile z in v.Zeilen)
            {
                if (z.Zone < 0 || z.Zone >= liste.Count) continue;
                int k = stelle[z.Zone]++;
                if (k < liste[z.Zone].Count && z.Quelltyp != null && !string.IsNullOrEmpty(z.Kennung))
                    liste[z.Zone][k].Add(Quellkennung.Kuerzen(z.Kennung));
            }
            return liste;
        }

        /// <summary>
        /// <b>Die Paarung</b>: je Zone des Vorschlags die Bestandszone mit den meisten gemeinsamen Kennungen (Räume, GUID), je
        /// Bauteil das Bestandsbauteil derselben Kennung (zuerst in der gepaarten Zone). Was aus der Datei stammt und nicht
        /// wiedergefunden wird, entfällt; Sätze ohne Kennung sind von Hand und bleiben.
        /// </summary>
        private static Paarung Paaren(Bestand alt, GebaeudeBauteilvorschlag neu)
        {
            var p = new Paarung { ZoneAlt = new ZoneModel[neu.Zonen.Count], BauteilAlt = new BauteilModel[neu.Zonen.Count][] };
            List<HashSet<string>> zk = ZonenKennungenNeu(neu);
            var frei = alt.Zonen.Where(alt.AusDatei).ToList();
            for (int i = 0; i < neu.Zonen.Count; i++)
            {
                ZoneModel best = null;
                int treffer = 0;
                foreach (ZoneModel a in frei)
                {
                    int n = alt.ZonenKennungen[a.ID].Count(zk[i].Contains);
                    if (n > treffer) { treffer = n; best = a; }
                }
                if (best != null) { p.ZoneAlt[i] = best; frei.Remove(best); }
            }
            p.ZonenEntfallend.AddRange(frei);
            p.ZonenHand.AddRange(alt.Zonen.Where(z => !alt.AusDatei(z)));

            List<List<HashSet<string>>> bk = BauteilKennungenNeu(neu);
            var freiB = alt.Zonen.SelectMany(z => z.Bauteile).Where(alt.AusDatei).ToList();
            for (int i = 0; i < neu.Zonen.Count; i++)
            {
                p.BauteilAlt[i] = new BauteilModel[neu.Zonen[i].Bauteile.Count];
                for (int k = 0; k < p.BauteilAlt[i].Length; k++)
                {
                    HashSet<string> kenn = bk[i][k];
                    if (kenn.Count == 0) continue;
                    BauteilModel b = freiB.FirstOrDefault(x => p.ZoneAlt[i] != null && x.ID_Zone == p.ZoneAlt[i].ID && alt.BauteilKennungen[x.ID].Overlaps(kenn))
                                     ?? freiB.FirstOrDefault(x => alt.BauteilKennungen[x.ID].Overlaps(kenn));
                    if (b != null) { p.BauteilAlt[i][k] = b; freiB.Remove(b); }
                }
            }
            p.BauteileEntfallend.AddRange(freiB);
            p.BauteileHand.AddRange(alt.Zonen.SelectMany(z => z.Bauteile).Where(b => !alt.AusDatei(b)));
            return p;
        }

        // ----- Regel je Feld: Hand > Datei > Vorgabe -----

        /// <summary>Hat der Anwender die Zone von Hand bearbeitet (Herkunft <c>MANUELL</c> am Satz, MA-2 setzt sie)?</summary>
        private static bool Hand(ZoneModel z) => string.Equals(z?.Herkunft, DbWerte.HERKUNFT_MANUELL, StringComparison.Ordinal);

        private static bool Hand(BauteilModel b) => string.Equals(b?.Herkunft, DbWerte.HERKUNFT_MANUELL, StringComparison.Ordinal);

        /// <summary>
        /// Der Aufbau des Bauteils ist von Hand gewählt: ein Satz <c>MANUELL</c> oder ein Aufbau, der nicht aus der Datei stammt.
        /// MA-6b: Nennt die Herkunft des U-Werts den Import (<c>IFC</c>, <c>GBXML</c>, <c>SQPROJ</c>) oder eine Vorgabe, kam der
        /// Aufbau mit dem Import (Ersatzaufbau) und ist nicht von Hand; ohne Herkunft (NULL, Bestand) gilt die Regel wie bisher.
        /// </summary>
        private static bool HandAufbau(Bestand alt, BauteilModel b)
            => Hand(b) || (b.ID_Aufbau.HasValue && !alt.DateiAufbauten.Contains(b.ID_Aufbau.Value) && !UAusImport(b));

        /// <summary>Stammt der U-Wert laut seiner Herkunft aus dem Import (Datei oder Vorgabe)?</summary>
        private static bool UAusImport(BauteilModel b)
            => b.U_Herkunft is DbWerte.HERKUNFT_IFC or DbWerte.HERKUNFT_GBXML or DbWerte.HERKUNFT_SQPROJ or DbWerte.HERKUNFT_VORGABE;

        /// <summary>
        /// Der U-Wert ist von Hand gesetzt (MA-6b, E149): Herkunft <c>MANUELL</c> am U-Wert (<c>Tab_Bauteil.U_Herkunft</c>) — auch
        /// ohne Aufbauwechsel. Eine unbekannte Herkunft (NULL, Bestand) zählt nicht.
        /// </summary>
        private static bool HandU(BauteilModel b) => string.Equals(b?.U_Herkunft, DbWerte.HERKUNFT_MANUELL, StringComparison.Ordinal);

        /// <summary>Die Fläche ist von Hand geändert: aus der Datei gepaart, ihre Flächenherkunft fiel in der Pflege auf <c>null</c>.</summary>
        private static bool HandFlaeche(BauteilModel alt, BauteilModel neu)
            => GebaeudeZonenanschluss.FlaechenherkunftVorhanden() && alt.Flaechenherkunft == null && neu.Flaechenherkunft != null;

        /// <summary>
        /// <b>Zone zusammenführen</b> (die Id des Bestands bleibt): Name, Fläche, Höhe und Volumen kommen immer aus der Datei.
        /// Eine Zone von Hand (<c>MANUELL</c>) behält alle übrigen Felder; sonst gilt je Feld der Wert der Datei, wo sie einen
        /// trägt, und der Bestand, wo sie keinen trägt (Übergabe, Vorheizzeit, Kühlung …).
        /// </summary>
        private static ZoneModel ZoneZusammen(ZoneModel alt, ZoneModel neu)
        {
            ZoneModel m = Hand(alt) ? alt.Kopie() : neu.Kopie();
            m.ID = alt.ID;
            m.ID_Gebaeude = alt.ID_Gebaeude;
            m.Bezeichner = neu.Bezeichner;
            m.Nutzflaeche = neu.Nutzflaeche;
            m.Raumhoehe = neu.Raumhoehe;
            m.Volumen = neu.Volumen;
            m.Quellkennung = neu.Quellkennung ?? alt.Quellkennung;
            if (Hand(alt)) return m;
            m.Raumsolltemperatur_Tag ??= alt.Raumsolltemperatur_Tag;
            m.Raumsolltemperatur_Nachtabsenkung ??= alt.Raumsolltemperatur_Nachtabsenkung;
            m.Raumsolltemperatur_Wochenende ??= alt.Raumsolltemperatur_Wochenende;
            m.Raumsolltemperatur_Ferien ??= alt.Raumsolltemperatur_Ferien;
            m.Maximaleraumtemperatur ??= alt.Maximaleraumtemperatur;
            m.Heizung_Strahlungsanteil ??= alt.Heizung_Strahlungsanteil;
            m.Heizleistung_Max ??= alt.Heizleistung_Max;
            m.Luftwechsel_Infiltration ??= alt.Luftwechsel_Infiltration;
            m.Luftwechsel_Nutzer ??= alt.Luftwechsel_Nutzer;
            m.Interne_Waermegewinne ??= alt.Interne_Waermegewinne;
            m.Bewohner ??= alt.Bewohner;
            m.Kuehl_Sollwert ??= alt.Kuehl_Sollwert;
            m.Kuehlleistung_Max ??= alt.Kuehlleistung_Max;
            m.Kuehlung_Aktiv ??= alt.Kuehlung_Aktiv;
            m.Kuehl_Sollwert_Nacht ??= alt.Kuehl_Sollwert_Nacht;
            if (neu.Uebergabe_Art == null)
            {
                m.Uebergabe_Art = alt.Uebergabe_Art;
                m.Uebergabe_Exponent = alt.Uebergabe_Exponent;
                m.Uebergabe_Leistung_Nenn = alt.Uebergabe_Leistung_Nenn;
                m.Auslegung_Vorlauf = alt.Auslegung_Vorlauf;
                m.Auslegung_Ruecklauf = alt.Auslegung_Ruecklauf;
                m.Auslegung_Raumtemperatur = alt.Auslegung_Raumtemperatur;
                m.Regler_Proportionalband = alt.Regler_Proportionalband;
            }
            if (neu.Kuehl_Uebergabe_Art == null)
            {
                m.Kuehl_Uebergabe_Art = alt.Kuehl_Uebergabe_Art;
                m.Kuehl_Uebergabe_Exponent = alt.Kuehl_Uebergabe_Exponent;
                m.Kuehl_Uebergabe_Leistung_Nenn = alt.Kuehl_Uebergabe_Leistung_Nenn;
            }
            m.Vorheizzeit_H ??= alt.Vorheizzeit_H;
            m.Nutzungsprofil ??= alt.Nutzungsprofil;
            return m;
        }

        /// <summary>
        /// <b>Bauteil zusammenführen</b> (die Id des Bestands bleibt): Name, Art, Neigung, Azimut und Fläche kommen aus der
        /// Datei — eine von Hand geänderte Fläche bleibt. Aufbau und U-Wert samt Herkunft bleiben, wenn der Aufbau von Hand
        /// gewählt oder der U-Wert von Hand gesetzt ist (<see cref="HandU"/>); ein
        /// Satz <c>MANUELL</c> behält zudem g-Wert, Rahmen, Verschattung, ψ·L und Randbedingung samt Nachbarzone.
        /// </summary>
        private static BauteilModel BauteilZusammen(Bestand bestand, BauteilModel alt, BauteilModel neu)
        {
            BauteilModel m = neu.Kopie();
            m.ID = alt.ID;
            m.Quellkennung = neu.Quellkennung ?? alt.Quellkennung;
            if (HandFlaeche(alt, neu)) { m.Flaeche = alt.Flaeche; m.Flaechenherkunft = null; }
            if (HandAufbau(bestand, alt) || HandU(alt)) { m.ID_Aufbau = alt.ID_Aufbau; m.U_Wert = alt.U_Wert; m.U_Herkunft = alt.U_Herkunft; }
            if (Hand(alt))
            {
                m.g_Wert = alt.g_Wert;
                m.Rahmenanteil = alt.Rahmenanteil;
                m.Verschattungsfaktor = alt.Verschattungsfaktor;
                m.Psi_L = alt.Psi_L;
                m.Randbedingung = alt.Randbedingung;
                m.ID_Nachbarzone = alt.ID_Nachbarzone;
                m.Trennflaeche_Zuordnung = alt.Trennflaeche_Zuordnung;
                m.Herkunft = alt.Herkunft;
            }
            return m;
        }

        // ----- Vorschau -----

        /// <summary>
        /// <b>Die Vorschau des Wiederimports</b> (MA-6): je Zone und Bauteil neu / geändert (welche Felder) / unverändert /
        /// entfällt / bleibt (Hand), ohne zu schreiben. Ein Gebäude ohne Zonen zeigt alles als neu.
        /// </summary>
        internal static Wiederimportvorschau Vorschau(int idGebaeude, GebaeudeImportHerkunft herkunft)
        {
            if (herkunft?.Vorschlag == null) return new Wiederimportvorschau(false, MyResource.Resource.GEBMZ_MSG_OHNE_ZONEN, Array.Empty<Wiederimportelement>());
            try
            {
                Bestand alt = BestandLesen(idGebaeude);
                GebaeudeBauteilvorschlag neu = herkunft.Vorschlag;
                Paarung p = Paaren(alt, neu);
                var liste = new List<Wiederimportelement>();
                for (int i = 0; i < neu.Zonen.Count; i++)
                {
                    ZoneModel n = neu.Zonen[i], a = p.ZoneAlt[i];
                    string zone = n.Bezeichner?.Trim() ?? "";
                    if (a == null) liste.Add(Element(true, zone, zone, Wiederimportstand.Neu, null, null));
                    else
                    {
                        var felder = new List<string>();
                        if (!string.Equals(a.Bezeichner?.Trim(), zone, StringComparison.Ordinal)) felder.Add(MyResource.Resource.GEBWI_FELD_BEZEICHNER);
                        if (!Gleich(a.Nutzflaeche, n.Nutzflaeche)) felder.Add(MyResource.Resource.GEBWI_FELD_FLAECHE);
                        if (!Gleich(a.Raumhoehe, n.Raumhoehe)) felder.Add(MyResource.Resource.GEBWI_FELD_HOEHE);
                        if (!Gleich(a.Volumen, n.Volumen)) felder.Add(MyResource.Resource.GEBWI_FELD_VOLUMEN);
                        var hand = new List<string>();
                        if (Hand(a)) hand.Add(MyResource.Resource.GEBWI_FELD_PROFIL);
                        if (!Hand(a) && a.Uebergabe_Art != null && n.Uebergabe_Art == null) hand.Add(MyResource.Resource.GEBWI_FELD_UEBERGABE);
                        liste.Add(Element(true, zone, zone, felder.Count > 0 ? Wiederimportstand.Geaendert : Wiederimportstand.Unveraendert, felder, hand));
                    }
                    for (int k = 0; k < n.Bauteile.Count; k++)
                    {
                        BauteilModel nb = n.Bauteile[k], ab = p.BauteilAlt[i][k];
                        string name = nb.Bezeichner?.Trim() ?? "";
                        if (ab == null) { liste.Add(Element(false, zone, name, Wiederimportstand.Neu, null, null)); continue; }
                        var felder = new List<string>();
                        var hand = new List<string>();
                        if (!string.Equals(ab.Bezeichner?.Trim(), name, StringComparison.Ordinal)) felder.Add(MyResource.Resource.GEBWI_FELD_BEZEICHNER);
                        if (!string.Equals(ab.Bauteilart, nb.Bauteilart, StringComparison.Ordinal)) felder.Add(MyResource.Resource.GEBWI_FELD_ART);
                        if (HandFlaeche(ab, nb)) hand.Add(MyResource.Resource.GEBWI_FELD_FLAECHE);
                        else if (!Gleich(ab.Flaeche, nb.Flaeche)) felder.Add(MyResource.Resource.GEBWI_FELD_FLAECHE);
                        if (HandU(ab)) hand.Add(MyResource.Resource.GEBWI_FELD_U);
                        else if (HandAufbau(alt, ab)) hand.Add(MyResource.Resource.GEBWI_FELD_AUFBAU);
                        else if (!Gleich(ab.U_Wert, nb.U_Wert)) felder.Add(MyResource.Resource.GEBWI_FELD_U);
                        if (!Gleich(ab.Neigung, nb.Neigung)) felder.Add(MyResource.Resource.GEBWI_FELD_NEIGUNG);
                        if (!Gleich(ab.Azimut, nb.Azimut)) felder.Add(MyResource.Resource.GEBWI_FELD_AZIMUT);
                        if (Hand(ab)) hand.Add(MyResource.Resource.GEBWI_FELD_EINGABEN);
                        else if (!string.Equals(ab.Randbedingung, nb.Randbedingung, StringComparison.Ordinal)) felder.Add(MyResource.Resource.GEBWI_FELD_RANDBEDINGUNG);
                        liste.Add(Element(false, zone, name, felder.Count > 0 ? Wiederimportstand.Geaendert : Wiederimportstand.Unveraendert, felder, hand));
                    }
                }
                string ZoneVon(int id) => alt.Zonen.FirstOrDefault(z => z.ID == id)?.Bezeichner ?? "";
                foreach (ZoneModel z in p.ZonenEntfallend) liste.Add(Element(true, z.Bezeichner, z.Bezeichner, Wiederimportstand.Entfaellt, null, null));
                foreach (BauteilModel b in p.BauteileEntfallend) liste.Add(Element(false, ZoneVon(b.ID_Zone), b.Bezeichner, Wiederimportstand.Entfaellt, null, null));
                foreach (ZoneModel z in p.ZonenHand) liste.Add(Element(true, z.Bezeichner, z.Bezeichner, Wiederimportstand.Bleibt, null, null));
                foreach (BauteilModel b in p.BauteileHand) liste.Add(Element(false, ZoneVon(b.ID_Zone), b.Bezeichner, Wiederimportstand.Bleibt, null, null));
                return new Wiederimportvorschau(true, "", liste);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return new Wiederimportvorschau(false, Formatieren(MyResource.Resource.GEBWI_MSG_VORSCHAU_FEHLER, ex.Message), Array.Empty<Wiederimportelement>());
            }
        }

        private static Wiederimportelement Element(bool zone, string zonenname, string name, Wiederimportstand stand,
                                                   List<string> felder, List<string> hand)
            => new Wiederimportelement(zone, zonenname ?? "", name ?? "", stand,
                                       (IReadOnlyList<string>)felder ?? Array.Empty<string>(), (IReadOnlyList<string>)hand ?? Array.Empty<string>());

        private static bool Gleich(double? a, double? b)
            => a.HasValue == b.HasValue && (!a.HasValue || Math.Abs(a.Value - b.Value) <= Math.Max(RAND, RAND * Math.Abs(a.Value)));

        // ----- Schreiben -----

        /// <summary>
        /// <b>Führt den Wiederimport mit dem Bestand zusammen</b> (MA-6, Entwurf Geführter Modellaufbau 2.6) — in EINEM Vorgang
        /// mit Rückweg bei Fehler. Zonen und Bauteile werden über ihre Quellkennung (<c>Tab_Importzuordnung</c>, Spalte
        /// <c>Quellkennung</c>) wiedergefunden und behalten ihre Id; Geometrie und Dateiwerte kommen aus der Datei, die
        /// Ergänzungen des Anwenders bleiben (Regel je Feld: <see cref="ZoneZusammen"/>, <see cref="BauteilZusammen"/>;
        /// Kalender, Wochen und Vorgabezeilen einer Zone von Hand bleiben, sonst kommen die der Datei). Sätze ohne
        /// Quellkennung bleiben unberührt. Was aus der Datei stammt und in ihr fehlt, entfernt nur
        /// <paramref name="entfallendeEntfernen"/>; sonst bleibt es (ohne Paarung).
        /// </summary>
        internal static Mehrzonenersetzung Zusammenfuehren(int idGebaeude, GebaeudeImportHerkunft herkunft, bool entfallendeEntfernen)
        {
            if (herkunft?.Vorschlag == null || herkunft.Quelle == null)
                return Mehrzonenersetzung.Fehler(MyResource.Resource.GEBMZ_MSG_OHNE_ZONEN);
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
                {
                    object projekt = v.Skalar("SELECT COALESCE(\"ID_Projekt\", 0) FROM \"Tab_Gebaeude\" WHERE \"ID\" = ?",
                                              new DbParam("@g", idGebaeude));
                    int idProjekt = projekt == null || projekt == DBNull.Value ? 0 : Convert.ToInt32(projekt, CultureInfo.InvariantCulture);
                    if (idProjekt <= 0)
                    {
                        v.Rollback();
                        return Mehrzonenersetzung.Fehler(Formatieren(MyResource.Resource.GEBMZ_MSG_KEINE_PROJEKTKOPIE,
                                                                    idGebaeude.ToString(CultureInfo.InvariantCulture)));
                    }

                    // 1) Der Bestand und die Paarung - bevor etwas geschrieben ist.
                    Bestand alt = BestandLesen(idGebaeude);
                    GebaeudeBauteilvorschlag vorschlag = herkunft.Vorschlag;
                    Paarung p = Paaren(alt, vorschlag);

                    // 2) Die alten Quellen samt Paarungen und Grundrissen; die Herkunft schreibt eine neue.
                    v.Ausfuehren("DELETE FROM \"" + ImportzuordnungSchema.TAB_QUELLE + "\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", idGebaeude));

                    // 3) Baustoffe und der Vorschlag NEBEN den Bestand - derselbe Schreibweg wie beim Anlegen.
                    string fehler = null;
                    if (herkunft.Baustoffzuordnungen is { Count: > 0 } zuordnungen)
                    {
                        BaustoffabgleichCtrl.Ergebnis b = BaustoffabgleichCtrl.Schreiben(idProjekt, zuordnungen, v);
                        if (!b.Ok) fehler = b.Meldung ?? "";
                    }
                    GebaeudeZonenCtrl.Vorschlagsergebnis z = null;
                    if (fehler == null)
                    {
                        z = new GebaeudeZonenCtrl().VorschlagSchreiben(idGebaeude, vorschlag, v, bestandZulassen: true);
                        if (!z.Ok) fehler = z.Meldung ?? "";
                    }
                    if (fehler == null)
                    {
                        // 4) Zusammenführen: die neuen Sätze auf die wiedergefundenen.
                        (Dictionary<int, int> zonenwahl, Dictionary<int, int> bauteilwahl) = Zusammenfuehren(v, alt, p, z, entfallendeEntfernen);

                        // 5) Gebäudekonditionierung und Herkunft (Paarungen auf die Ids, die bleiben).
                        if (herkunft.Gebaeudekonditionierung != null || herkunft.Gebaeudeprofil != null)
                            fehler = ZonenplanCtrl.ProjektdateiUebernehmen(idGebaeude, null, herkunft.Gebaeudekonditionierung, herkunft.Gebaeudeprofil);
                        if (fehler == null)
                        {
                            IEnumerable<GebaeudeQuellzuordnung> paarungen = (herkunft.Paarungen ?? Array.Empty<GebaeudeQuellzuordnung>())
                                .Concat(z.Zuordnungen.Select(q => Umgeschrieben(q, zonenwahl, bauteilwahl)));
                            GebaeudeImportCtrl.Ergebnis h = new GebaeudeImportCtrl().SchreibeHerkunft(
                                idGebaeude, herkunft.Quelle, paarungen.ToList(), v, herkunft.Raumgrundrisse);
                            if (!h.Ok) fehler = h.Meldung ?? "";
                        }
                        // 6) Die Aufbauten der alten Datei, die kein Bauteil mehr trägt.
                        if (fehler == null)
                            foreach (int a in alt.DateiAufbauten)
                                v.Ausfuehren("DELETE FROM \"" + BauteilaufbauSchema.TAB_AUFBAU + "\" WHERE \"ID\" = ? AND NOT EXISTS " +
                                             "(SELECT 1 FROM \"" + ZonenSchema.TAB_BAUTEIL + "\" WHERE \"ID_Aufbau\" = ?)",
                                             new DbParam("@a", a), new DbParam("@b", a));
                    }
                    if (fehler != null)
                    {
                        v.Rollback();
                        return Mehrzonenersetzung.Fehler(Formatieren(MyResource.Resource.GEBMZ_MSG_NICHT_UEBERNOMMEN, fehler));
                    }
                    v.Commit();
                    return new Mehrzonenersetzung(true, "", vorschlag.Zonen.Count);
                }
            }
            catch (Exception ex)
            {
                return Mehrzonenersetzung.Fehler(Formatieren(MyResource.Resource.GEBMZ_MSG_NICHT_UEBERNOMMEN, ex.Message));
            }
        }

        private static GebaeudeQuellzuordnung Umgeschrieben(GebaeudeQuellzuordnung q, Dictionary<int, int> zonen, Dictionary<int, int> bauteile)
        {
            if (!q.ZielId.HasValue) return q;
            Dictionary<int, int> wahl = q.Ziel == ImportZiel.Zone ? zonen : q.Ziel == ImportZiel.Bauteil ? bauteile : null;
            return wahl != null && wahl.TryGetValue(q.ZielId.Value, out int id) ? new GebaeudeQuellzuordnung(q.Quelltyp, q.Quellkennung, q.Ziel, id) : q;
        }

        /// <summary>
        /// Der Schreibteil der Zusammenführung im Vorgang <paramref name="v"/>: neue Sätze auf die wiedergefundenen, Kalender je
        /// Zone nach der Regel, Entfallendes nach Wahl, Rangfolge neu. Liefert die Abbildung neue Id → bleibende Id je Zone und
        /// Bauteil.
        /// </summary>
        private static (Dictionary<int, int> Zonen, Dictionary<int, int> Bauteile) Zusammenfuehren(
            DbVorgang v, Bestand alt, Paarung p, GebaeudeZonenCtrl.Vorschlagsergebnis z, bool entfallendeEntfernen)
        {
            IReadOnlyList<ZoneModel> neu = z.Zonen ?? new[] { z.Zone };
            var zonenwahl = new Dictionary<int, int>();
            var bauteilwahl = new Dictionary<int, int>();
            for (int i = 0; i < neu.Count; i++) zonenwahl[neu[i].ID] = p.ZoneAlt[i]?.ID ?? neu[i].ID;
            int Ziel(int? id) => id.HasValue && zonenwahl.TryGetValue(id.Value, out int t) ? t : id ?? 0;

            // a) Die Zonen: die wiedergefundene nimmt die Felder nach der Regel; ihre Kalender bleiben (Hand) oder kommen neu.
            bool kalender = KonditionierungSchema.Lesbar();
            bool wochen = DataRepository.TabelleVorhanden(KalenderbedienungSchema.TAB_WOCHE);
            for (int i = 0; i < neu.Count; i++)
            {
                ZoneModel a = p.ZoneAlt[i];
                if (a == null) continue;
                GebaeudeZonenCtrl.ZoneAktualisieren(v, ZoneZusammen(a, neu[i]));
                if (!kalender || Hand(a)) continue;
                foreach (string tab in KalenderTabellen(wochen))
                {
                    v.Ausfuehren("DELETE FROM \"" + tab + "\" WHERE \"ID_Zone\" = ?", new DbParam("@a", a.ID));
                    v.Ausfuehren("UPDATE \"" + tab + "\" SET \"ID_Zone\" = ? WHERE \"ID_Zone\" = ?", new DbParam("@a", a.ID), new DbParam("@n", neu[i].ID));
                }
            }

            // b) Die Bauteile: das wiedergefundene nimmt die Felder nach der Regel, das neue zieht in die bleibende Zone.
            var reihe = new Dictionary<int, List<int>>();
            for (int i = 0; i < neu.Count; i++)
            {
                int zone = zonenwahl[neu[i].ID];
                if (!reihe.TryGetValue(zone, out List<int> r)) reihe[zone] = r = new List<int>();
                for (int k = 0; k < neu[i].Bauteile.Count; k++)
                {
                    BauteilModel nb = neu[i].Bauteile[k].Kopie();
                    nb.ID_Zone = zone;
                    if (nb.ID_Nachbarzone.HasValue) nb.ID_Nachbarzone = Ziel(nb.ID_Nachbarzone);
                    BauteilModel ab = p.BauteilAlt[i][k];
                    if (ab == null)
                    {
                        GebaeudeZonenCtrl.BauteilAktualisieren(v, nb);
                        r.Add(nb.ID);
                        continue;
                    }
                    BauteilModel m = BauteilZusammen(alt, ab, nb);
                    v.Ausfuehren("DELETE FROM \"" + ZonenSchema.TAB_BAUTEIL + "\" WHERE \"ID\" = ?", new DbParam("@n", nb.ID));
                    GebaeudeZonenCtrl.BauteilAktualisieren(v, m);
                    bauteilwahl[nb.ID] = m.ID;
                    r.Add(m.ID);
                }
            }

            // c) Die neuen Zonen, deren Platz eine wiedergefundene einnimmt, sind nun leer.
            foreach (ZoneModel n in neu.Where(n => zonenwahl[n.ID] != n.ID))
            {
                v.Ausfuehren("UPDATE \"" + ZonenSchema.TAB_BAUTEIL + "\" SET \"ID_Nachbarzone\" = ? WHERE \"ID_Nachbarzone\" = ?",
                             new DbParam("@a", zonenwahl[n.ID]), new DbParam("@n", n.ID));
                v.Ausfuehren("DELETE FROM \"" + ZonenSchema.TAB_ZONE + "\" WHERE \"ID\" = ?", new DbParam("@n", n.ID));
            }

            // d) Entfallendes nur mit Zustimmung: erst die Bauteile, dann die Zonen samt ihrem Inhalt.
            if (entfallendeEntfernen)
            {
                foreach (BauteilModel b in p.BauteileEntfallend)
                    v.Ausfuehren("DELETE FROM \"" + ZonenSchema.TAB_BAUTEIL + "\" WHERE \"ID\" = ?", new DbParam("@b", b.ID));
                foreach (ZoneModel e in p.ZonenEntfallend)
                {
                    v.Ausfuehren("UPDATE \"" + ZonenSchema.TAB_BAUTEIL + "\" SET \"ID_Nachbarzone\" = NULL WHERE \"ID_Nachbarzone\" = ?", new DbParam("@e", e.ID));
                    v.Ausfuehren("DELETE FROM \"" + ZonenSchema.TAB_BAUTEIL + "\" WHERE \"ID_Zone\" = ?", new DbParam("@e", e.ID));
                    v.Ausfuehren("DELETE FROM \"" + ZonenSchema.TAB_ZONE + "\" WHERE \"ID\" = ?", new DbParam("@e", e.ID));
                }
            }

            // e) Die Rangfolge: die Zonen der Datei in ihrer Reihenfolge, danach die übrigen; je Zone die Bauteile ebenso.
            var gesetzt = new HashSet<int>();
            int rang = 0;
            foreach (ZoneModel n in neu)
            {
                int id = zonenwahl[n.ID];
                if (gesetzt.Add(id)) Rang(v, ZonenSchema.TAB_ZONE, id, ++rang);
            }
            DataTable rest = v.Lese("SELECT \"ID\" FROM \"" + ZonenSchema.TAB_ZONE + "\" WHERE \"ID_Gebaeude\" = ? ORDER BY \"Rang\", \"ID\"",
                                    new DbParam("@g", neu[0].ID_Gebaeude));
            var zonenAlle = rest.Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r[0], CultureInfo.InvariantCulture)).ToList();
            foreach (int id in zonenAlle.Where(id => !gesetzt.Contains(id))) Rang(v, ZonenSchema.TAB_ZONE, id, ++rang);
            foreach (int zone in zonenAlle)
            {
                reihe.TryGetValue(zone, out List<int> datei);
                datei ??= new List<int>();
                var ids = v.Lese("SELECT \"ID\" FROM \"" + ZonenSchema.TAB_BAUTEIL + "\" WHERE \"ID_Zone\" = ? ORDER BY \"Rang\", \"ID\"", new DbParam("@z", zone))
                           .Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r[0], CultureInfo.InvariantCulture)).ToList();
                int rb = 0;
                foreach (int id in datei.Concat(ids.Where(x => !datei.Contains(x)))) Rang(v, ZonenSchema.TAB_BAUTEIL, id, ++rb);
            }
            return (zonenwahl, bauteilwahl);
        }

        private static IEnumerable<string> KalenderTabellen(bool wochen)
        {
            yield return KonditionierungSchema.TAB_KALENDER;
            if (wochen) yield return KalenderbedienungSchema.TAB_WOCHE;
            yield return KonditionierungSchema.TAB_VORGABE;
        }

        private static void Rang(DbVorgang v, string tabelle, int id, int rang)
            => v.Ausfuehren("UPDATE \"" + tabelle + "\" SET \"Rang\" = ? WHERE \"ID\" = ?", new DbParam("@r", rang), new DbParam("@i", id));
    }
}
