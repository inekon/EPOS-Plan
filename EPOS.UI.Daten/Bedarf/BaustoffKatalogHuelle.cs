using System;
using System.Collections.Generic;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die HÜLLE der Verwaltung „Baustoffe" (Gebäudesimulation G3, Welle C; Softwarearchitektur
    /// 1.2 und 1.4) — plattformfrei: Sie baut den Parametersatz des
    /// <see cref="BaustoffKatalogDialog"/> aus <see cref="BaustoffCtrl"/> und schreibt das Ergebnis
    /// über denselben Controller zurück.
    ///
    /// <para><b>Beide Schalen nehmen dieselbe Hülle:</b> Die Verwaltung ist eine freie Ansicht der
    /// <c>AppWurzel</c> (Seitenschlüssel <c>BAUSTOFF_KATALOG</c>); unter Windows reicht die
    /// Hauptfensterhülle <see cref="Gaben"/> als Delegat herein, auf iOS die Projektquelle. Ein
    /// Fenster in der Schale gibt es nicht (Architektur 3.1).</para>
    ///
    /// <para><b>Die Hülle rechnet nicht und prüft nicht selbst</b> — sie übersetzt zwischen dem DTO
    /// der Oberfläche (<see cref="BaustoffDaten"/>) und dem Modell des Kerns
    /// (<see cref="BaustoffModel"/>); die Regeln stehen in <see cref="BaustoffCtrl.Pruefen"/>.</para>
    /// </summary>
    internal static class BaustoffKatalogHuelle
    {
        /// <summary>Der Parametersatz der Komponente — ohne <c>Geschlossen</c>, das setzt der Wirt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben()
        {
            var ctrl = new BaustoffCtrl();
            return new Dictionary<string, object>
            {
                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(BaustoffCtrl.Katalogfilterzeilen),
                ["Katalogprofil"] = Katalogfilterprofil.FuerBaustoff(Katalogtexte.Fuer),
                ["Lies"] = new Func<int, BaustoffDaten>(id => AlsDaten(ctrl.LesenKatalogsatz(id))),
                ["Pruefen"] = new Func<BaustoffDaten, string>(d => BaustoffCtrl.Pruefen(AlsModell(d, null))),
                ["Speichern"] = new Func<BaustoffDaten, BaustoffSpeicherErgebnis>(d => Speichern(ctrl, d)),
                ["Loeschen"] = new Func<int, BaustoffSpeicherErgebnis>(id => Abbild(ctrl.KatalogLoeschen(id))),
                ["Duplizieren"] = new Func<int, string, BaustoffSpeicherErgebnis>(
                    (id, name) => Abbild(ctrl.KatalogDuplizieren(id, name))),
                ["Schloss"] = Schlosswege.Aus(BaustoffCtrl.SchlossSetzen),
                ["Verwendung"] = new Func<IReadOnlyDictionary<int, int>>(BaustoffCtrl.KatalogVerwendung)
            };
        }

        /// <summary>Anlegen (Id 0) oder Ändern — der Kern prüft noch einmal und schützt die Auslieferung.</summary>
        internal static BaustoffSpeicherErgebnis Speichern(BaustoffCtrl ctrl, BaustoffDaten d)
        {
            if (d == null) return new BaustoffSpeicherErgebnis(false, MyResource.Resource.BAUSTOFF_MSG_NAME_LEER, 0);
            BaustoffModel alt = d.Id > 0 ? ctrl.LesenKatalogsatz(d.Id) : null;
            BaustoffModel m = AlsModell(d, alt);
            return Abbild(m.ID > 0 ? ctrl.KatalogAendern(m) : ctrl.KatalogAnlegen(m));
        }

        /// <summary>Das Kernmodell als DTO der Oberfläche; <c>null</c> bleibt <c>null</c>.</summary>
        internal static BaustoffDaten AlsDaten(BaustoffModel m)
        {
            if (m == null) return null;
            return new BaustoffDaten
            {
                Id = m.ID,
                Bezeichner = m.Bezeichner ?? "",
                Gruppe = m.Gruppe ?? "",
                Hersteller = m.Hersteller ?? "",
                Lambda = m.Lambda,
                Rho = m.Rho,
                Cp = m.Cp,
                Quelle = m.Quelle ?? "",
                Herkunft = BaustoffCtrl.HerkunftText(m.Herkunft),
                Quellkennung = m.Quellkennung ?? "",
                Auslieferung = m.ReadOnly
            };
        }

        /// <summary>
        /// Das DTO als Kernmodell — Herkunft und Quellkennung bleiben die des gespeicherten Satzes
        /// <paramref name="alt"/> (der Kern setzt sie beim Anlegen selbst; die Prüfung braucht sie
        /// nicht und fragt deshalb keine Datenbank), leere Texte werden NULL.
        /// </summary>
        internal static BaustoffModel AlsModell(BaustoffDaten d, BaustoffModel alt)
        {
            if (d == null) return null;
            return new BaustoffModel
            {
                ID = d.Id,
                Bezeichner = d.Bezeichner ?? "",
                Gruppe = Leer(d.Gruppe),
                Hersteller = Leer(d.Hersteller),
                Lambda = d.Lambda,
                Rho = d.Rho,
                Cp = d.Cp,
                Quelle = Leer(d.Quelle),
                Herkunft = alt?.Herkunft,
                Quellkennung = alt?.Quellkennung,
                ReadOnly = alt?.ReadOnly ?? false
            };
        }

        private static BaustoffSpeicherErgebnis Abbild(BaustoffCtrl.Ergebnis e)
            => e == null ? new BaustoffSpeicherErgebnis(false, "", 0) : new BaustoffSpeicherErgebnis(e.Ok, e.Meldung ?? "", e.Id);

        private static string Leer(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
