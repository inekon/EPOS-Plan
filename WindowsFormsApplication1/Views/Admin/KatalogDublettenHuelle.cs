using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Admin;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Katalog-Dublettensuche (iU9-W14c.5).
    ///
    /// <para><b>Die Datenbankseite steht hier, nicht in der Komponente.</b> Der Scan
    /// kommt aus <see cref="DublettenPruefung"/>, der Baum aus
    /// <see cref="DublettenBaum"/> (W14c.0h), der Detailtext aus
    /// <see cref="DublettenBefundText"/> (W14c.0f) und die drei Schreibwege aus
    /// <see cref="KatalogBereinigung"/>. <b>Die Hülle hält das Scanergebnis</b> —
    /// es führt <c>DataRow</c>-Objekte, und die dürfen die Komponente nicht
    /// erreichen.</para>
    ///
    /// <para><b>Der Scan läuft in <c>Task.Run</c></b> (A-15, Befund W14c-B41): Bei
    /// „(alle Kataloge)" sind das 19 Volltabellen-Lesungen samt Hashbildung. In einer
    /// WebView ist der Renderfaden derselbe Faden; der Vorläufer half sich mit
    /// <c>_lblStatus.Refresh()</c>.</para>
    ///
    /// <para><b>Der Knotenschlüssel ist die gemeinsame Sprache</b> von Komponente und
    /// Hülle: <c>K:&lt;Katalog&gt;</c>, <c>…/N</c> bzw. <c>…/I</c> für den Ast,
    /// <c>…/&lt;Gruppenindex&gt;</c> und <c>…/&lt;SatzId&gt;</c>. Die Hülle löst ihn
    /// gegen ihr Scanergebnis auf — ein Index über die Anzeige hätte nach jedem
    /// Neuscan eine andere Bedeutung.</para>
    /// </summary>
    internal static class KatalogDublettenHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 1 000 × 640, Mindestmaß 840 × 540).</summary>
        private static readonly Size MASS = new Size(1040, 700);

        /// <summary>Der vorgeschlagene Dateiname des Protokolls (wörtlich).</summary>
        private const string PROTOKOLLDATEI = "KatalogDubletten.txt";

        /// <summary>
        /// Das Scanergebnis der laufenden Sitzung, je Registry-Schlüssel — dasselbe
        /// Feld, das der Vorläufer als <c>_ergebnisse</c> führte.
        /// </summary>
        private static Dictionary<string, ScanErgebnis> _ergebnisse =
            new Dictionary<string, ScanErgebnis>(StringComparer.Ordinal);

        /// <summary>
        /// Zeigt die Dublettensuche als eigenes Fenster — der Weg von
        /// <c>Hauptfensterrahmen.InitDublettenMenue</c>.
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<KatalogDublettenDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben())
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<KatalogDublettenDialog>(
                MyResource.Resource.ADM_DUBLETTEN_TITEL, MASS, werte);

            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>Der PARAMETERSATZ der Komponente.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            _ergebnisse = new Dictionary<string, ScanErgebnis>(StringComparer.Ordinal);

            return new Dictionary<string, object>
            {
                ["Kataloge"] = (IReadOnlyList<(string, string)>)KatalogRegistry.Alle
                    .Select(k => (k.Schluessel, KatalogRegistry.Anzeige(k.Schluessel)))
                    .ToList(),
                ["Scannen"] = new Func<string, IProgress<Scanmeldung>, Task<Scanergebnis>>(Scannen),
                ["Detailtext"] = new Func<string, Task<string>>(Detailtext),
                ["Bereinigungsumfang"] = new Func<string, Task<int>>(Bereinigungsumfang),
                ["Bereinigen"] = new Func<string, Task<Aktionsergebnis>>(Bereinigen),
                ["LoeschPruefen"] = new Func<string, Task<Loeschpruefung>>(LoeschPruefen),
                ["Loeschen"] = new Func<string, Task<Aktionsergebnis>>(Loeschen),
                ["UmbenennenVorbereiten"] = new Func<string, Task<Umbenennung>>(UmbenennenVorbereiten),
                ["NamePruefen"] = new Func<string, string, string>(NamePruefen),
                ["Umbenennen"] = new Func<string, string, Task<Aktionsergebnis>>(Umbenennen),
                ["ProtokollSpeichern"] = new Func<IReadOnlyList<string>, Task<string>>(ProtokollSpeichern)
            };
        }

        // =====================================================================
        // Scan (btnPruefen_Click / Scannen / StatusNachScan)
        // =====================================================================

        private static async Task<Scanergebnis> Scannen(string schluessel, IProgress<Scanmeldung> melder)
        {
            var ziele = new List<KatalogDefinition>();
            if (string.IsNullOrEmpty(schluessel))
            {
                ziele.AddRange(KatalogRegistry.Alle);
            }
            else
            {
                KatalogDefinition k = KatalogRegistry.Finde(schluessel);
                if (k != null) ziele.Add(k);
            }

            var protokoll = new List<string>();

            // A-15: Der Scan laeuft im Hintergrund. Das Marshalling besorgt Progress<T>
            // (auf dem Bedienfaden erzeugt, uebernimmt dessen SynchronizationContext).
            await Task.Run(() =>
            {
                for (int i = 0; i < ziele.Count; i++)
                {
                    KatalogDefinition k = ziele[i];
                    if (melder != null)
                        melder.Report(new Scanmeldung(
                            ziele.Count > 1 ? (double)i / ziele.Count : (double?)null,
                            string.Format(MyResource.Resource.ADM_DUBLETTEN_STATUS_PRUEFE,
                                          KatalogRegistry.Anzeige(k.Schluessel))));

                    ScanErgebnis erg = DublettenPruefung.ScanKatalog(k);
                    _ergebnisse[k.Schluessel] = erg;
                    if (erg.Fehler != null) protokoll.Add(k.Tabelle + ": " + erg.Fehler);
                }
            });

            return new Scanergebnis(DublettenBaum.Bauen(_ergebnisse), StatusNachScan(), protokoll);
        }

        /// <summary>
        /// Die Statuszeile nach dem Scan: „Keine Dubletten gefunden." oder LEER — dann
        /// spricht der Baum.
        /// </summary>
        private static string StatusNachScan()
        {
            int gruppen = 0;
            foreach (ScanErgebnis erg in _ergebnisse.Values)
            {
                if (erg.Fehler != null) continue;
                gruppen += erg.Namensgruppen.Count;
                gruppen += DublettenBaum.AnzuzeigendeInhaltsgruppen(erg).Count;
            }
            return gruppen == 0 ? MyResource.Resource.ADM_DUBLETTEN_KEINE : "";
        }

        // =====================================================================
        // Knotenschluessel aufloesen
        // =====================================================================

        /// <summary>Ein aufgelöster Knoten: Katalog, Gruppe und Satz — wie <c>KnotenInfo</c>.</summary>
        private sealed class Knoten
        {
            internal KatalogDefinition Katalog;
            internal DublettenGruppe Gruppe;
            internal KatalogSatz Satz;
        }

        /// <summary>
        /// Löst einen Knotenschlüssel gegen das Scanergebnis auf. Aufbau:
        /// <c>K:&lt;Katalog&gt;[/N|/I[/&lt;Gruppe&gt;[/&lt;SatzId&gt;]]]</c>.
        /// </summary>
        private static Knoten Aufloesen(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel) || !schluessel.StartsWith("K:", StringComparison.Ordinal))
                return null;

            string[] teile = schluessel.Substring(2).Split('/');
            KatalogDefinition k = KatalogRegistry.Finde(teile[0]);
            if (k == null) return null;

            var knoten = new Knoten { Katalog = k };
            ScanErgebnis erg;
            if (teile.Length < 3 || !_ergebnisse.TryGetValue(k.Schluessel, out erg) || erg.Fehler != null)
                return knoten;

            IReadOnlyList<DublettenGruppe> gruppen = teile[1] == "N"
                ? (IReadOnlyList<DublettenGruppe>)erg.Namensgruppen
                : DublettenBaum.AnzuzeigendeInhaltsgruppen(erg);

            int index;
            if (!int.TryParse(teile[2], out index) || index < 0 || index >= gruppen.Count) return knoten;
            knoten.Gruppe = gruppen[index];

            if (teile.Length < 4) return knoten;

            int id;
            if (!int.TryParse(teile[3], out id)) return knoten;
            foreach (KatalogSatz s in knoten.Gruppe.Saetze)
                if (s.Id == id) { knoten.Satz = s; break; }
            return knoten;
        }

        // =====================================================================
        // Detailtext (DetailText / Zelle - jetzt DublettenBefundText, W14c.0f)
        // =====================================================================

        private static Task<string> Detailtext(string schluessel)
        {
            Knoten n = Aufloesen(schluessel);
            if (n == null) return Task.FromResult("");

            var sb = new StringBuilder();

            if (n.Satz != null)
            {
                foreach ((string spalte, string wert) in DublettenBefundText.Blatt(n.Katalog, n.Satz))
                    sb.Append(spalte).Append(" = ").Append(wert).AppendLine();
                return Task.FromResult(sb.ToString());
            }

            if (n.Gruppe != null)
            {
                IReadOnlyList<Gegenueberstellung> paare =
                    DublettenBefundText.Gruppe(n.Katalog, n.Gruppe);
                for (int i = 0; i < paare.Count; i++)
                {
                    Gegenueberstellung g = paare[i];
                    if (i > 0) sb.AppendLine();
                    sb.Append("ID ").Append(g.IdA).Append(" \"").Append(g.NameA).Append("\"  |  ")
                      .Append("ID ").Append(g.IdB).Append(" \"").Append(g.NameB).Append("\"").AppendLine();
                    foreach ((string spalte, string a, string b) in g.Zeilen)
                        sb.Append(spalte).Append(": ").Append(a).Append(" | ").Append(b).AppendLine();
                }
            }

            return Task.FromResult(sb.ToString());
        }

        // =====================================================================
        // Bereinigen (btnBereinigen_Click)
        // =====================================================================

        private static Task<int> Bereinigungsumfang(string schluessel)
        {
            Knoten n = Aufloesen(schluessel);
            if (n == null || n.Katalog == null) return Task.FromResult(0);
            if (n.Gruppe != null) return Task.FromResult(1);

            ScanErgebnis erg;
            return Task.FromResult(
                _ergebnisse.TryGetValue(n.Katalog.Schluessel, out erg) && erg.Fehler == null
                    ? erg.Namensgruppen.Count : 0);
        }

        private static Task<Aktionsergebnis> Bereinigen(string schluessel)
        {
            Knoten n = Aufloesen(schluessel);
            if (n == null || n.Katalog == null) return Task.FromResult(Aktionsergebnis.Nichts());

            BereinigungsErgebnis berg;
            if (n.Gruppe != null)
            {
                berg = new BereinigungsErgebnis();
                KatalogBereinigung.GruppeBereinigen(n.Katalog, n.Gruppe, berg);
            }
            else
            {
                berg = KatalogBereinigung.LeereKopienBereinigen(n.Katalog);
            }

            return Task.FromResult(new Aktionsergebnis(berg.Geloescht > 0, berg.Protokoll));
        }

        // =====================================================================
        // Loeschen (btnLoeschen_Click) - die drei Schranken
        // =====================================================================

        private static Task<Loeschpruefung> LoeschPruefen(string schluessel)
        {
            Knoten n = Aufloesen(schluessel);
            if (n == null || n.Satz == null)
                return Task.FromResult(new Loeschpruefung(false, false, "", "", "", 0));

            if (n.Satz.ReadOnly)
                return Task.FromResult(new Loeschpruefung(true, false, "", "", n.Satz.Name, n.Satz.Id));

            if (n.Katalog.VerwendungsPruefungen.Length == 0)
                return Task.FromResult(new Loeschpruefung(false, true, "", "", n.Satz.Name, n.Satz.Id));

            var treffer = new List<string>();
            foreach (VerwendungsPruefung vp in n.Katalog.VerwendungsPruefungen)
            {
                string fehler;
                int anzahl = KatalogBereinigung.VerwendungZaehlen(vp, n.Satz, out fehler);

                // Befund W14c-B44: Ein Fehlschlag ist NICHT "nicht verwendet".
                if (anzahl < 0)
                    return Task.FromResult(new Loeschpruefung(false, false, "", fehler ?? "",
                                                              n.Satz.Name, n.Satz.Id));
                if (anzahl > 0) treffer.Add(vp.Tabelle + " (" + anzahl + ")");
            }

            return Task.FromResult(new Loeschpruefung(false, false, string.Join(", ", treffer.ToArray()),
                                                      "", n.Satz.Name, n.Satz.Id));
        }

        private static Task<Aktionsergebnis> Loeschen(string schluessel)
        {
            Knoten n = Aufloesen(schluessel);
            if (n == null || n.Satz == null) return Task.FromResult(Aktionsergebnis.Nichts());

            bool ok = KatalogBereinigung.SatzLoeschen(n.Katalog, n.Satz.Id);
            string zeile = n.Katalog.Tabelle + ", ID " + n.Satz.Id + " \"" + n.Satz.Name + "\": " +
                           (ok ? MyResource.Resource.ADM_DUBLETTEN_PROT_GELOESCHT
                               : MyResource.Resource.ADM_DUBLETTEN_PROT_LOESCHEN_FEHLER);

            return Task.FromResult(new Aktionsergebnis(ok, new[] { zeile },
                ok ? "" : MyResource.Resource.ADM_DUBLETTEN_PROT_LOESCHEN_FEHLER));
        }

        // =====================================================================
        // Umbenennen (btnUmbenennen_Click / NameErfragen)
        // =====================================================================

        private static Task<Umbenennung> UmbenennenVorbereiten(string schluessel)
        {
            Knoten n = Aufloesen(schluessel);
            if (n == null || n.Satz == null) return Task.FromResult(new Umbenennung(false, ""));
            return Task.FromResult(new Umbenennung(n.Satz.ReadOnly, n.Satz.Name));
        }

        /// <summary>
        /// „Normalisiert schon vergeben — außer es ist der EIGENE Name". Der eigene
        /// bleibt erlaubt, damit sich Schreibweisen (Leerzeichen, Groß/Klein)
        /// korrigieren lassen (Konzept 4.3).
        /// </summary>
        private static string NamePruefen(string schluessel, string name)
        {
            Knoten n = Aufloesen(schluessel);
            if (n == null || n.Satz == null) return null;

            string norm = DublettenPruefung.NormalisiereName(name);
            if (string.Equals(norm, n.Satz.NameNormalisiert, StringComparison.Ordinal)) return null;

            return DublettenPruefung.VergebeneNamen(n.Katalog).Contains(norm)
                ? string.Format(MyResource.Resource.IMP_KONFLIKT_NAME_UNGUELTIG, name)
                : null;
        }

        private static Task<Aktionsergebnis> Umbenennen(string schluessel, string neu)
        {
            Knoten n = Aufloesen(schluessel);
            if (n == null || n.Satz == null) return Task.FromResult(Aktionsergebnis.Nichts());

            bool ok = KatalogBereinigung.SatzUmbenennen(n.Katalog, n.Satz.Id, neu);
            string zeile = n.Katalog.Tabelle + ", ID " + n.Satz.Id + ": \"" + n.Satz.Name +
                           "\" -> \"" + neu + "\"" +
                           (ok ? "" : " " + MyResource.Resource.ADM_DUBLETTEN_PROT_UMBENENNEN_FEHLER);

            return Task.FromResult(new Aktionsergebnis(ok, new[] { zeile },
                ok ? "" : MyResource.Resource.ADM_DUBLETTEN_PROT_UMBENENNEN_FEHLER));
        }

        // =====================================================================
        // Protokoll (btnProtokoll_Click)
        // =====================================================================

        /// <summary>
        /// Schreibt das Sitzungsprotokoll. <b>CRLF je Zeile, UTF-8</b> — lesbar in
        /// Editor und Excel, wörtlich wie der Vorläufer.
        /// </summary>
        private static Task<string> ProtokollSpeichern(IReadOnlyList<string> zeilen)
        {
            try
            {
                string pfad = Dienste.Datei.DateiSpeichern(
                    MyResource.Resource.ADM_DUBLETTEN_BTN_PROTOKOLL, "*.txt|*.txt", PROTOKOLLDATEI);
                if (string.IsNullOrEmpty(pfad)) return Task.FromResult("");

                File.WriteAllText(pfad, string.Join("\r\n", zeilen.ToArray()) + "\r\n", Encoding.UTF8);
                return Task.FromResult(MyResource.Resource.ADM_DUBLETTEN_MSG_PROTOKOLL_GESPEICHERT);
            }
            catch (Exception ex)
            {
                return Task.FromResult(ex.Message);
            }
        }
    }
}
