using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Filterzustand EINER Katalogliste</b> — je Spalte ein Ausdruck, dazu die
    /// Suche und die Sortierung (Konzept_Katalogfilter 5.6.6 und 5.6.8).
    ///
    /// <para><b>Er lebt im WIRT, nicht im Popover.</b> Der <c>@key</c>-Fix W6-B-2 baut
    /// das QuickGrid neu auf, sobald die Zeilenzahl wechselt — und beim Filtern
    /// wechselt sie praktisch immer. Ein Zustand im Popover waere nach dem ersten
    /// Tastendruck weg (offener Punkt O-6).</para>
    /// </summary>
    public sealed class Katalogfilterstand
    {
        private readonly Dictionary<string, string> _spalten =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Das EINE Suchfeld ueber alle Spalten (Zone A).</summary>
        public string Suche { get; set; } = "";

        /// <summary>
        /// Nach welcher Spalte sortiert wird; leer = die Reihenfolge des Controllers
        /// (in allen acht Katalogen <c>ORDER BY Bezeichner</c>).
        /// </summary>
        public string Sortierspalte { get; set; } = "";

        /// <summary>Aufsteigend? Nur sinnvoll, solange <see cref="Sortierspalte"/> belegt ist.</summary>
        public bool Aufsteigend { get; set; } = true;

        /// <summary>Der Ausdruck einer Spalte; leer heisst „kein Filter".</summary>
        public string Ausdruck(string schluessel)
        {
            string a;
            return _spalten.TryGetValue(schluessel ?? "", out a) ? a : "";
        }

        /// <summary>Setzt den Ausdruck einer Spalte; leerer Text nimmt den Filter zurueck.</summary>
        public void Setzen(string schluessel, string ausdruck)
        {
            if (string.IsNullOrEmpty(schluessel)) return;
            string a = (ausdruck ?? "").Trim();
            if (a.Length == 0) _spalten.Remove(schluessel);
            else _spalten[schluessel] = a;
        }

        /// <summary>Traegt die Spalte einen Filter? Das ist der GEFUELLTE Trichter (5.6.2).</summary>
        public bool Gefiltert(string schluessel) => Ausdruck(schluessel).Length > 0;

        /// <summary>
        /// Ist mindestens EIN Spaltenfilter gesetzt? Genau daran haengt der Textknopf
        /// „Filter zuruecksetzen" (5.6.4) — die Suche zaehlt nicht dazu, sie hat ihr
        /// eigenes Feld.
        /// </summary>
        public bool Gesetzt => _spalten.Count > 0;

        /// <summary>Die Zahl der gesetzten Spaltenfilter (Pruefhilfe).</summary>
        public int Anzahl => _spalten.Count;

        /// <summary>
        /// „Filter zuruecksetzen": nimmt ALLE Spaltenfilter zurueck. Die Suche und die
        /// Sortierung bleiben — der Knopf ist der Ruecksetzer der Trichter, nicht des
        /// Suchfeldes (5.6.4: „Er ist kein Filter, sondern ein Ruecksetzer").
        /// </summary>
        public void Zuruecksetzen() => _spalten.Clear();

        /// <summary>
        /// Der Sortierzyklus EINER Spalte: auf → ab → aus (5.6.2). Es ist immer
        /// hoechstens EINE Spalte sortiert; ein Klick auf eine andere beginnt dort
        /// wieder mit „auf".
        /// </summary>
        public void Sortieren(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return;

            if (Sortierspalte != schluessel) { Sortierspalte = schluessel; Aufsteigend = true; return; }
            if (Aufsteigend) { Aufsteigend = false; return; }

            Sortierspalte = "";
            Aufsteigend = true;
        }
    }

    /// <summary>
    /// <b>Die RECHNUNG des Katalogfilters</b> (Konzept_Katalogfilter Kapitel 3,
    /// Anwenderentscheid W14a-E-10) — ohne Oberflaeche pruefbar.
    ///
    /// <para><b>Gefiltert wird VOR dem Raster</b> (5.6.6). <c>Raster.razor</c> ist eine
    /// duenne Huelle um QuickGrid, und <b>QuickGrid filtert nicht</b>: Der Wirt legt
    /// den Ausdruck in den <see cref="Katalogfilterstand"/>, ruft
    /// <c>…StammCtrl.Katalogfilterzeilen()</c>, laesst diese Klasse die Menge
    /// einschraenken und reicht die BEREITS EINGESCHRAENKTE Liste als
    /// <c>Zeilen</c> weiter. Fuer die 20 749 PV-Module heisst das: Das Raster bekommt
    /// nach dem Filtern 15 Zeilen und virtualisiert gar nicht mehr — genau der
    /// Uebergang, den der Fix W6-B-2 abdeckt.</para>
    ///
    /// <para><b>Die Verknuepfung</b> (5.6.3): Die Spaltenfilter wirken <b>UND</b> (jede
    /// gesetzte Spalte muss passen), das Suchfeld <b>ODER ueber alle Spalten</b> und
    /// <b>UND ueber mehrere Begriffe</b> — die Regel von
    /// <see cref="VdiAuswahlFilter.Passt"/>. Suche und Spaltenfilter gelten
    /// gleichzeitig.</para>
    ///
    /// <para><b>Wiederverwendet, nicht abgeschrieben.</b> Die Teilsuche kommt aus
    /// <see cref="VdiAuswahlFilter.Passt"/> (gross/klein egal ueber
    /// <c>CurrentCultureIgnoreCase</c>, mehrere Begriffe als UND), die Platzhalter
    /// <c>*</c> und <c>?</c> aus <see cref="Suchmuster.Uebersetzen"/> (mit Platzhalter
    /// verankert, ohne Teilsuche, kaputtes Muster = kein Filter), der Zahlenausdruck
    /// aus <see cref="Zahlenausdruck.Lesen(string, CultureInfo)"/>.</para>
    /// </summary>
    public static class Katalogfilter
    {
        /// <summary>
        /// Die eingeschraenkte und sortierte Liste. <paramref name="stand"/> darf
        /// <c>null</c> sein — dann steht die ungefilterte Liste in der Reihenfolge des
        /// Controllers da.
        /// </summary>
        public static IReadOnlyList<Katalogfilterzeile> Anwenden(
            Katalogfilterprofil profil,
            IReadOnlyList<Katalogfilterzeile> zeilen,
            Katalogfilterstand stand)
        {
            if (zeilen == null) return Array.Empty<Katalogfilterzeile>();
            if (profil == null || stand == null) return zeilen;

            var treffer = new List<Katalogfilterzeile>(zeilen.Count);
            for (int i = 0; i < zeilen.Count; i++)
                if (Passt(profil, zeilen[i], stand)) treffer.Add(zeilen[i]);

            return Sortieren(profil, treffer, stand);
        }

        /// <summary>
        /// Passt EINE Zeile? Erst die Spaltenfilter (UND), dann die Suche (ODER ueber
        /// die Spalten, UND ueber die Begriffe).
        /// </summary>
        public static bool Passt(Katalogfilterprofil profil, Katalogfilterzeile zeile,
                                 Katalogfilterstand stand)
        {
            if (zeile == null) return false;
            if (profil == null || stand == null) return true;

            for (int i = 0; i < profil.Spalten.Count; i++)
            {
                Katalogspalte spalte = profil.Spalten[i];
                string ausdruck = stand.Ausdruck(spalte.Schluessel);
                if (ausdruck.Length == 0) continue;
                if (!PasstSpalte(spalte, zeile.Wert(spalte.Schluessel), ausdruck)) return false;
            }

            return PasstSuche(profil, zeile, stand.Suche);
        }

        /// <summary>
        /// Der Filter EINER Spalte auf EINEN Wert.
        ///
        /// <para><b>Zahlenspalte:</b> <see cref="Zahlenausdruck"/>. Ein Ausdruck, den er
        /// nicht versteht, ist KEIN Filter — dieselbe Regel wie beim kaputten
        /// Suchmuster; der Anwender tippt gerade.</para>
        /// <para><b>Text- und Kennzeichenspalte:</b> Teilzeichenkette ueber
        /// <see cref="VdiAuswahlFilter.Passt"/>; mit <c>*</c> oder <c>?</c> geht der
        /// Ausdruck durch <see cref="Suchmuster"/>.</para>
        /// </summary>
        public static bool PasstSpalte(Katalogspalte spalte, Katalogwert wert, string ausdruck)
        {
            string a = (ausdruck ?? "").Trim();
            if (a.Length == 0) return true;
            if (spalte == null) return true;

            if (spalte.Art == Katalogspaltenart.Zahl)
            {
                Zahlenbedingung bedingung = Zahlenausdruck.Lesen(a);
                if (bedingung == null) return true;          // unverstanden = kein Filter
                return bedingung.Trifft(wert.Zahl);
            }

            return TextTrifft(a, wert.Text);
        }

        /// <summary>
        /// Das EINE Suchfeld der Zone A: ODER ueber alle Spalten, UND ueber die durch
        /// Leerzeichen getrennten Begriffe.
        /// </summary>
        public static bool PasstSuche(Katalogfilterprofil profil, Katalogfilterzeile zeile,
                                      string suche)
        {
            string s = (suche ?? "").Trim();
            if (s.Length == 0) return true;
            if (profil == null || zeile == null) return false;

            string[] felder = new string[profil.Spalten.Count];
            for (int i = 0; i < profil.Spalten.Count; i++)
                felder[i] = zeile.Text(profil.Spalten[i].Schluessel);

            // OHNE Platzhalter ist es woertlich die Regel von VdiAuswahlFilter.Passt -
            // sie wird gerufen, nicht abgeschrieben.
            if (s.IndexOf('*') < 0 && s.IndexOf('?') < 0)
                return VdiAuswahlFilter.Passt(s, felder);

            // MIT Platzhalter: je Begriff ein Suchmuster (verankert), UND ueber die
            // Begriffe, ODER ueber die Felder - dieselbe Verknuepfung wie oben.
            string[] begriffe = s.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (int b = 0; b < begriffe.Length; b++)
                if (!TextTrifft(begriffe[b], felder)) return false;

            return true;
        }

        /// <summary>
        /// Die sortierte Liste. Ohne Sortierspalte bleibt die Reihenfolge des
        /// Controllers stehen; Gleichstand loest der Bezeichner auf — dieselbe Regel
        /// wie in <c>ProjektListe</c> seit W15a.
        /// </summary>
        public static IReadOnlyList<Katalogfilterzeile> Sortieren(
            Katalogfilterprofil profil, List<Katalogfilterzeile> zeilen,
            Katalogfilterstand stand)
        {
            if (zeilen == null) return Array.Empty<Katalogfilterzeile>();
            if (profil == null || stand == null || stand.Sortierspalte.Length == 0) return zeilen;

            Katalogspalte spalte = profil.Spalte(stand.Sortierspalte);
            if (spalte == null || !spalte.Sortierbar) return zeilen;

            string schluessel = spalte.Schluessel;
            bool zahl = spalte.Art == Katalogspaltenart.Zahl ||
                        spalte.Art == Katalogspaltenart.JaNein;
            int richtung = stand.Aufsteigend ? 1 : -1;

            // List.Sort ist NICHT stabil; der Gleichstand wird deshalb ausdruecklich
            // ueber den Bezeichner aufgeloest, damit dieselbe Liste immer dieselbe
            // Reihenfolge ergibt (Determinismus - dieselbe Regel wie im Bericht).
            zeilen.Sort((a, b) =>
            {
                int v;
                if (zahl)
                {
                    double? za = a.Zahl(schluessel);
                    double? zb = b.Zahl(schluessel);

                    // LEERWERTE STEHEN HINTEN — in BEIDEN Richtungen, deshalb VOR der
                    // Richtungsumkehr. Ein Satz ohne gepflegten Wert ist kein „kleinster"
                    // Satz; er gehoert ans Ende der Liste, egal wie herum sortiert wird.
                    if (za == null && zb == null) v = 0;
                    else if (za == null) return 1;
                    else if (zb == null) return -1;
                    else v = za.Value.CompareTo(zb.Value);
                }
                else
                {
                    v = VergleicheText(a.Text(schluessel), b.Text(schluessel));
                }

                if (v != 0) return richtung * v;
                return string.Compare(a.Bezeichner, b.Bezeichner, StringComparison.CurrentCulture);
            });
            return zeilen;
        }

        // =================================================================
        // Hilfen
        // =================================================================

        /// <summary>
        /// Trifft ein Ausdruck einen einzelnen Text? Mit Platzhalter ueber
        /// <see cref="Suchmuster"/>, ohne ueber <see cref="VdiAuswahlFilter.Passt"/>.
        /// </summary>
        private static bool TextTrifft(string ausdruck, string text)
        {
            return TextTrifft(ausdruck, new[] { text ?? "" });
        }

        /// <summary>Trifft ein Ausdruck EINES der Felder?</summary>
        private static bool TextTrifft(string ausdruck, string[] felder)
        {
            if (ausdruck.IndexOf('*') < 0 && ausdruck.IndexOf('?') < 0)
                return VdiAuswahlFilter.Passt(ausdruck, felder);

            Regex muster = Suchmuster.Uebersetzen(ausdruck);
            if (muster == null) return true;            // kaputtes Muster = kein Filter

            for (int f = 0; f < felder.Length; f++)
                if (Suchmuster.Trifft(muster, felder[f])) return true;
            return false;
        }

        /// <summary>
        /// Text in der Kultur des Anwenders — „Öl" steht damit bei „O" und nicht hinter
        /// „Z" (dieselbe Wahl wie <see cref="VdiAuswahlFilter.Passt"/>, das ebenfalls
        /// mit <c>CurrentCulture</c> vergleicht).
        /// </summary>
        private static int VergleicheText(string a, string b)
        {
            return string.Compare(a ?? "", b ?? "", StringComparison.CurrentCulture);
        }
    }
}
