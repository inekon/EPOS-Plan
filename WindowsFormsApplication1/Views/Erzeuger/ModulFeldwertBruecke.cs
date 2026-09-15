using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Erzeuger;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Brücke vom ZWILLINGSSYSTEM des Modulkatalogs auf den Aufklapper „Alle
    /// Daten anzeigen"</b> (Anwenderentscheid 15.09.2026, „alle sechs Erzeuger im
    /// gleichen Schema").
    ///
    /// <para><b>Warum es sie gibt.</b> Der Aufklapper im Modulbereich ist bei allen
    /// Familien derselbe Baustein — <c>Katalogfelder</c>, gespeist aus
    /// <see cref="BrowserFeldwert"/>. Heizkessel und BHKW bekommen ihre Liste vom
    /// Katalogbrowser (<c>KatalogBrowserWege</c>); Photovoltaik und Stromspeicher haben
    /// keinen Browser, sondern den Modulkatalog mit seinem eigenen Feldtyp
    /// (<see cref="ModulFeldwert"/> und <see cref="ModulKatalogWege"/>). Diese Klasse
    /// bildet den einen Typ auf den anderen ab — an EINER Stelle, damit die zwei
    /// Familien nicht je ihre eigene Abschrift führen.</para>
    ///
    /// <para><b>Die Abbildung ist beinahe die Identität.</b> Beide Feldtypen tragen
    /// Schlüssel, Beschriftung, Einheit, Wert und dieselbe Aufzählung
    /// <see cref="BrowserFeldArt"/>; die Feldart geht also unverändert durch. Zwei
    /// Angaben haben kein Gegenstück und bleiben in der Modulliste zurück:
    /// <c>ModulFeldwert.LeerErlaubt</c> (die Pflichtprüfung des Katalogeditors) und
    /// <c>ModulFeldwert.Gruppe</c> (seine Feldgruppen). Beide betreffen die MASKE des
    /// Katalogeditors, nicht den Wert — der Aufklapper zeigt eine Liste ohne Gruppen,
    /// und geprüft wird beim Speichern im Kern.</para>
    ///
    /// <para><b>Was nicht editierbar wird.</b> Der Bezeichner ist der Schlüssel des
    /// <c>UPDATE</c> und im Profil bereits <c>Gesperrt</c>. Dazu kommt
    /// <see cref="BrowserFeldArt.Auswahl"/>: Ein Auswahlfeld führt seine Optionen
    /// (Wert = Datenbankcode, Text = Beschriftung) im <see cref="ModulFeldwert"/>,
    /// <see cref="BrowserFeldwert"/> kennt sie nicht, und der Baustein
    /// <c>Katalogfelder</c> zeichnete daraus ein freies Textfeld — der Anwender könnte
    /// einen Code eintippen, den es nicht gibt. Solche Felder stehen im Aufklapper
    /// deshalb als LESEWERT; gepflegt werden sie im Katalogeditor hinter
    /// „Bearbeiten…". Betroffen ist heute genau eines: die Zelltechnologie des
    /// PV-Moduls.</para>
    ///
    /// <para><b>Gespeichert wird der VOLLSATZ.</b> <c>ModulKatalogWege.Speichern</c>
    /// schreibt den ganzen Stammsatz, nicht die geänderten Spalten — deshalb liest der
    /// Rückweg den Satz frisch über <c>Detail</c>, überträgt die Werte der
    /// editierbaren Felder hinein und gibt IHN weiter. So bleiben die Spalten
    /// unangetastet, die der Aufklapper gar nicht zeigt oder nicht setzen darf.</para>
    /// </summary>
    internal static class ModulFeldwertBruecke
    {
        /// <summary>
        /// Die Felder eines Katalogsatzes als <see cref="BrowserFeldwert"/> —
        /// der Weg hinter dem Gaben-Schlüssel <c>Katalogfelder</c>.
        /// </summary>
        /// <returns><c>null</c>, wenn es den Bezeichner nicht gibt.</returns>
        internal static IReadOnlyList<BrowserFeldwert> Felder(ModulKatalogWege wege, string name)
        {
            if (wege == null || wege.Detail == null) return null;

            IReadOnlyList<ModulFeldwert> satz = wege.Detail(name);
            if (satz == null) return null;

            var liste = new List<BrowserFeldwert>(satz.Count);
            foreach (ModulFeldwert f in satz)
                liste.Add(new BrowserFeldwert
                {
                    Schluessel = f.Schluessel,
                    Bezeichnung = f.Bezeichnung,
                    Einheit = f.Einheit,
                    Art = f.Art,
                    Editierbar = Editierbar(f),
                    Wert = Anzeigewert(f)
                });

            return liste;
        }

        /// <summary>
        /// Der ANZEIGEWERT eines Feldes.
        /// </summary>
        /// <remarks>
        /// <b>Ein Auswahlfeld zeigt den Text seiner Option, nicht den Datenbankcode.</b>
        /// Der Code steht im Feld (<c>ModulFeldwert.Wert</c>), lesbar ist er nicht — der
        /// Katalogeditor zeichnet daraus eine Klappliste, der Aufklapper hat keine. Weil
        /// solche Felder nie zurückgeschrieben werden (siehe
        /// <see cref="Speichern"/> — der Rückweg liest den Satz frisch und rührt nur die
        /// editierbaren Felder an), kann hier gefahrlos der Text stehen. Ein Code ohne
        /// passende Option bleibt, wie er ist: Dann fällt er auf.
        /// </remarks>
        private static string Anzeigewert(ModulFeldwert feld)
        {
            string wert = feld.Wert ?? "";
            if (feld.Art != BrowserFeldArt.Auswahl) return wert;

            foreach (var option in feld.Optionen)
                if (string.Equals(option.Wert, wert, StringComparison.Ordinal)) return option.Text;

            return wert;
        }

        /// <summary>
        /// Schreibt die geänderten Felder zurück — der Weg hinter dem Gaben-Schlüssel
        /// <c>KatalogfelderSpeichern</c>.
        /// </summary>
        /// <remarks>
        /// <para><b>Der Satz wird frisch gelesen</b> und nicht aus dem Aufklapper
        /// zusammengebaut: Der Speicherweg des Modulkatalogs nimmt den VOLLSATZ
        /// entgegen, und der Aufklapper trägt nur, was er zeigen darf. Ein Feld, das
        /// die Liste von draußen nicht kennt (oder das nicht editierbar ist), behält
        /// damit seinen gelesenen Wert.</para>
        /// <para><b>Umbenannt wird nicht.</b> Der Bezeichner ist gesperrt, und
        /// <paramref name="name"/> geht als WHERE-Schlüssel des <c>UPDATE</c> mit —
        /// derselbe Aufruf, den der Katalogeditor beim Ändern macht
        /// (<c>neu: false</c>).</para>
        /// </remarks>
        internal static KatalogSpeicherErgebnis Speichern(
            ModulKatalogWege wege, string name, IReadOnlyList<BrowserFeldwert> felder)
        {
            if (wege == null || wege.Speichern == null || wege.Detail == null)
                return new KatalogSpeicherErgebnis(false, NichtGefunden(name), name ?? "");

            IReadOnlyList<ModulFeldwert> satz = wege.Detail(name);
            if (satz == null)
                return new KatalogSpeicherErgebnis(false, NichtGefunden(name), name ?? "");

            foreach (ModulFeldwert ziel in satz)
            {
                if (!Editierbar(ziel)) continue;

                BrowserFeldwert quelle = Suchen(felder, ziel.Schluessel);
                if (quelle == null) continue;

                ziel.Wert = quelle.Wert ?? "";
            }

            return wege.Speichern(satz, false, name);
        }

        /// <summary>
        /// Darf der Aufklapper dieses Feld setzen? Nicht der gesperrte Bezeichner und
        /// kein Auswahlfeld (siehe Klassenkommentar).
        /// </summary>
        private static bool Editierbar(ModulFeldwert feld)
        {
            return !feld.Gesperrt && feld.Art != BrowserFeldArt.Auswahl;
        }

        /// <summary>Das Feld mit diesem Schlüssel; <c>null</c>, wenn die Liste es nicht führt.</summary>
        private static BrowserFeldwert Suchen(IReadOnlyList<BrowserFeldwert> felder, string schluessel)
        {
            if (felder == null) return null;

            foreach (BrowserFeldwert f in felder)
                if (string.Equals(f.Schluessel, schluessel, StringComparison.Ordinal)) return f;

            return null;
        }

        /// <summary>
        /// Die Meldung, wenn der Satz zwischen Aufklappen und Speichern verschwunden
        /// ist. Der Text ist der vorhandene Katalogtext des Hauses — ein eigener
        /// Schlüssel für einen Fall, den es nur bei gleichzeitiger Pflege gibt, wäre
        /// ein Schlüssel zuviel.
        /// </summary>
        private static string NichtGefunden(string name)
        {
            string muster = null;
            try { muster = MyResource.Resource.ResourceManager.GetString("WRK_MSG_NICHT_GEFUNDEN"); }
            catch { }
            if (string.IsNullOrEmpty(muster))
                muster = "Der Katalogeintrag \"{0}\" wurde nicht gefunden.";

            return string.Format(muster, name ?? "");
        }
    }
}
