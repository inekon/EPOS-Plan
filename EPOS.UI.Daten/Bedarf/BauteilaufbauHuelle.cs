using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die HÜLLE der Verwaltung „Bauteilaufbauten" (Gebäudesimulation G3, Welle C;
    /// Softwarearchitektur 1.2 und 1.4) — plattformfrei: Sie baut den Parametersatz des
    /// <see cref="BauteilaufbauDialog"/> aus <see cref="BauteilaufbauCtrl"/> und
    /// <see cref="BaustoffCtrl"/> und schreibt Kopf und Schichten als EIN Aggregat zurück.
    ///
    /// <para><b>Beide Schalen nehmen dieselbe Hülle</b> (freie Ansicht der <c>AppWurzel</c>,
    /// Seitenschlüssel <c>BAUTEILAUFBAU_KATALOG</c>).</para>
    ///
    /// <para><b>Die Hülle rechnet nicht</b>: Den Summenfuß liefert
    /// <see cref="BauteilaufbauCtrl.Kennwerte"/>, die Prüfregeln
    /// <see cref="BauteilaufbauCtrl.EingabePruefen"/>. Sie übersetzt nur — auch die Schichtdicke,
    /// die die Oberfläche in mm zeigt und der Kern in m führt, und zwar genau hier und über den
    /// Kern (<see cref="BauteilaufbauCtrl.DickeMm"/>, <see cref="BauteilaufbauCtrl.DickeM"/>).</para>
    /// </summary>
    internal static class BauteilaufbauHuelle
    {
        /// <summary>Der Parametersatz der Komponente — ohne <c>Geschlossen</c>, das setzt der Wirt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            var ctrl = new BauteilaufbauCtrl();
            return new Dictionary<string, object>
            {
                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(BauteilaufbauCtrl.Katalogfilterzeilen),
                ["Katalogprofil"] = Katalogfilterprofil.FuerBauteilaufbau(Katalogtexte.Fuer),
                ["Lies"] = new Func<int, BauteilaufbauDaten>(id => AlsDaten(ctrl.LesenKatalogsatz(id))),
                ["Baustoffe"] = new Func<IReadOnlyList<BaustoffWahl>>(Baustoffe),
                ["Bauteilarten"] = Bauteilarten(),
                ["Kennwerte"] = new Func<BauteilaufbauDaten, AufbauKennwerteDaten>(Kennwerte),
                ["Pruefen"] = new Func<BauteilaufbauDaten, string>(d => BauteilaufbauCtrl.EingabePruefen(AlsModell(d))),
                ["Speichern"] = new Func<BauteilaufbauDaten, BauteilaufbauSpeicherErgebnis>(d => Speichern(ctrl, d)),
                ["Loeschen"] = new Func<int, BauteilaufbauSpeicherErgebnis>(id => Abbild(ctrl.KatalogLoeschen(id))),
                ["Duplizieren"] = new Func<int, string, BauteilaufbauSpeicherErgebnis>(
                    (id, name) => Abbild(ctrl.KatalogDuplizieren(id, name))),
                ["Schloss"] = Schlosswege.Aus(BauteilaufbauCtrl.SchlossSetzen)
            };
        }

        /// <summary>
        /// Die Bauteilarten der Auswahl — zuerst „für jede Bauteilart" (leerer Wert = NULL), dann
        /// die neun Persistenzwerte in Schemareihenfolge mit ihrem Anzeigetext aus dem Kern.
        /// </summary>
        internal static IReadOnlyList<(string Wert, string Text)> Bauteilarten()
        {
            var liste = new List<(string, string)> { ("", BauteilaufbauCtrl.BauteilartText(null)) };
            foreach (string art in DbWerte.BAUTEILARTEN) liste.Add((art, BauteilaufbauCtrl.BauteilartText(art)));
            return liste;
        }

        /// <summary>
        /// Die Baustoffe des Katalogs zur Wahl einer Schicht: „Name (Hersteller) · λ" — der
        /// Hersteller unterscheidet gleichnamige Stoffe, λ ist die Zahl, nach der gewählt wird.
        /// </summary>
        internal static IReadOnlyList<BaustoffWahl> Baustoffe()
        {
            var liste = new List<BaustoffWahl>();
            foreach (BaustoffModel b in new BaustoffCtrl().LesenKatalog())
            {
                string text = b.Bezeichner + (string.IsNullOrWhiteSpace(b.Hersteller) ? "" : " (" + b.Hersteller.Trim() + ")");
                if (b.Lambda.HasValue)
                    text += " · λ " + b.Lambda.Value.ToString("0.0###", CultureInfo.CurrentCulture);
                liste.Add(new BaustoffWahl(b.ID, text, b.Lambda, b.Rho, b.Cp));
            }
            return liste;
        }

        /// <summary>Der Summenfuß aus dem Kern, übersetzt in das DTO der Oberfläche.</summary>
        internal static AufbauKennwerteDaten Kennwerte(BauteilaufbauDaten d)
        {
            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(AlsModell(d));
            return new AufbauKennwerteDaten
            {
                NeigungGrad = k.NeigungGrad,
                RSi_M2KW = k.RSi_M2KW,
                RSe_M2KW = k.RSe_M2KW,
                RJeSchicht_M2KW = k.RJeSchicht_M2KW,
                R_M2KW = k.R_M2KW,
                U_WM2K = k.U_WM2K,
                Kapazitaet_KJM2K = k.Kapazitaet_KJM2K,
                KapazitaetWirksam_KJM2K = k.KapazitaetWirksam_KJM2K,
                Bezugsperiode_D = k.Bezugsperiode_D,
                R1Rel = k.R1Rel,
                C1Rel = k.C1Rel,
                Grund = k.Grund ?? "",
                PeriodeGrund = k.PeriodeGrund ?? ""
            };
        }

        /// <summary>Anlegen (Id 0) oder Ändern — Kopf und Schichten in EINER Transaktion des Kerns.</summary>
        internal static BauteilaufbauSpeicherErgebnis Speichern(BauteilaufbauCtrl ctrl, BauteilaufbauDaten d)
        {
            if (d == null) return new BauteilaufbauSpeicherErgebnis(false, MyResource.Resource.BAUTEIL_MSG_AUFBAU_NAME_LEER, 0);
            BauteilaufbauModel m = AlsModell(d);
            if (m.ID > 0)
            {
                // Herkunft und Quellkennung bleiben die des gespeicherten Aufbaus.
                BauteilaufbauModel alt = ctrl.LesenKatalogsatz(m.ID);
                m.Herkunft = alt?.Herkunft;
                m.Quellkennung = alt?.Quellkennung;
            }
            return Abbild(ctrl.KatalogSpeichern(m));
        }

        /// <summary>Das Kernmodell als DTO der Oberfläche — die Dicke in mm; <c>null</c> bleibt <c>null</c>.</summary>
        internal static BauteilaufbauDaten AlsDaten(BauteilaufbauModel m)
        {
            if (m == null) return null;
            return new BauteilaufbauDaten
            {
                Id = m.ID,
                Bezeichner = m.Bezeichner ?? "",
                Beschreibung = m.Beschreibung ?? "",
                Bauteilart = m.Bauteilart ?? "",
                Quelle = m.Quelle ?? "",
                Herkunft = BaustoffCtrl.HerkunftText(m.Herkunft),
                Quellkennung = m.Quellkennung ?? "",
                Auslieferung = m.ReadOnly,
                Schichten = m.Schichten.Where(s => s != null).Select(s => new BauteilschichtDaten
                {
                    Id = s.ID,
                    IdBaustoff = s.ID_Baustoff,
                    DickeMm = BauteilaufbauCtrl.DickeMm(s.Dicke),
                    IstLuftschicht = s.IstLuftschicht,
                    Lambda = s.Lambda,
                    Rho = s.Rho,
                    Cp = s.Cp
                }).ToList()
            };
        }

        /// <summary>
        /// Das DTO als Kernmodell — die Dicke in m, die Reihenfolge nach der Liste (lückenlos ab 1),
        /// leere Texte werden NULL, die Bauteilart „für jede" wird NULL. Ohne Datenbank.
        /// </summary>
        internal static BauteilaufbauModel AlsModell(BauteilaufbauDaten d)
        {
            if (d == null) return null;
            var m = new BauteilaufbauModel
            {
                ID = d.Id,
                Bezeichner = d.Bezeichner ?? "",
                Beschreibung = Leer(d.Beschreibung),
                Bauteilart = Leer(d.Bauteilart),
                Quelle = Leer(d.Quelle),
                ReadOnly = d.Auslieferung
            };
            int rang = 0;
            foreach (BauteilschichtDaten s in d.Schichten ?? new List<BauteilschichtDaten>())
            {
                if (s == null) continue;
                m.Schichten.Add(new BauteilschichtModel
                {
                    Reihenfolge = ++rang,
                    ID_Baustoff = s.IdBaustoff,
                    Dicke = s.DickeMm.HasValue ? BauteilaufbauCtrl.DickeM(s.DickeMm.Value) : 0.0,
                    IstLuftschicht = s.IstLuftschicht,
                    Lambda = s.Lambda,
                    Rho = s.Rho,
                    Cp = s.Cp
                });
            }
            return m;
        }

        private static BauteilaufbauSpeicherErgebnis Abbild(BauteilaufbauCtrl.Ergebnis e)
            => e == null ? new BauteilaufbauSpeicherErgebnis(false, "", 0)
                         : new BauteilaufbauSpeicherErgebnis(e.Ok, e.Meldung ?? "", e.Id);

        private static string Leer(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
