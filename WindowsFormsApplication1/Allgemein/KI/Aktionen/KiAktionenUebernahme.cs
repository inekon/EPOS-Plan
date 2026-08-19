using System;
using System.Collections.Generic;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die beiden Trockenlaeufe der Uebernahme (Fachkonzept 5.1, Zeilen 8-9).
    /// </summary>
    /// <remarks>
    /// Beide Aktionen SCHREIBEN NICHTS. Sie sind die Vorschau, die Fachkonzept 3.5 fuer die
    /// zugehoerigen Stufe-2-Aktionen verlangt - und in Etappe 1 fuer sich genommen
    /// nuetzlich, weil sie beantworten, ob eine Uebernahme ueberhaupt etwas bewirken wuerde.
    /// </remarks>
    internal static class KiAktionenUebernahme
    {
        // =====================================================================
        // uebernahme_vorschau
        // =====================================================================

        /// <summary>
        /// Trockenlauf der Komponenten-Uebernahme. Andockpunkt
        /// <c>KomponentenUebernahmeCtrl.Planen(int, int, string)</c>; zulaessige Gewerke aus
        /// <c>KomponentenUebernahmeCtrl.Plaene</c>, Pruefung ueber <c>Unterstuetzt(string)</c>.
        /// </summary>
        internal static KiAktion UebernahmeVorschau()
        {
            return new KiAktion(
                name: "uebernahme_vorschau",
                zweck: KiAktionsTexte.ZweckUebernahmeVorschau,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "KomponentenUebernahmeCtrl.Planen",
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlVonProjekt, name: "von_projekt",
                                             anzeigename: KiAktionsTexte.VonProjektName),
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlNachProjekt, name: "nach_projekt",
                                             anzeigename: KiAktionsTexte.NachProjektName),
                    new KiParameter("gewerk", KiParameterTyp.Aufzaehlung, KiAktionsTexte.ErlGewerk,
                                    anzeigename: KiAktionsTexte.GewerkName, werte: Gewerke())
                },
                vorbedingung: a =>
                {
                    int von = KiHilfe.ProjektId(a, "von_projekt");
                    int nach = KiHilfe.ProjektId(a, "nach_projekt");
                    string gewerk = a.Text("gewerk");

                    if (von == nach) return KiAktionsTexte.GleicheProjekte;

                    string grund = KiHilfe.ProjektMussAufloesbarSein(a, "von_projekt");
                    if (grund != null) return grund;
                    grund = KiHilfe.ProjektMussAufloesbarSein(a, "nach_projekt");
                    if (grund != null) return grund;

                    if (!KomponentenUebernahmeCtrl.Unterstuetzt(gewerk))
                        return string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.GewerkNichtUnterstuetzt,
                                             gewerk, string.Join(", ", Gewerke()));
                    return null;
                },
                ausfuehren: a =>
                {
                    int von = KiHilfe.ProjektId(a, "von_projekt");
                    int nach = KiHilfe.ProjektId(a, "nach_projekt");
                    string gewerk = a.Text("gewerk");

                    KomponentenUebernahmeCtrl.Vorschau v =
                        new KomponentenUebernahmeCtrl().Planen(von, nach, gewerk);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "von_projekt", von,
                        "nach_projekt", nach,
                        "gewerk", gewerk,
                        "moeglich", v.Moeglich,
                        "nichts_zu_tun", v.NichtsZuTun,
                        "anlegen", v.Anlegen.Count,
                        "ersetzen", v.Gleichziehen.Count,
                        "entfernen", v.Entfernen.Count,
                        "grund", KiHilfe.Text(v.Grund)));

                    string text = v.Moeglich
                        ? string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.UebernahmeMoeglich,
                                        v.Anlegen.Count, v.Gleichziehen.Count, v.Entfernen.Count)
                        : string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.UebernahmeNichtMoeglich, v.Grund);

                    KiErgebnis e = KiErgebnis.Ok(text, zeilen, anzahl: 1);
                    if (!string.IsNullOrWhiteSpace(v.Klartext)) e.MitMeldungen(new[] { v.Klartext.Trim() });
                    return e;
                });
        }

        /// <summary>
        /// Die unterstuetzten Gewerke - aus der Landkarte des Controllers, nicht aus einer
        /// zweiten Liste. Was dort fehlt, kann der Assistent nicht anbieten.
        /// </summary>
        private static string[] Gewerke()
        {
            var namen = new List<string>(KomponentenUebernahmeCtrl.Plaene.Keys);
            namen.Sort(StringComparer.Ordinal);
            return namen.ToArray();
        }

        // =====================================================================
        // merkmal_vorschau
        // =====================================================================

        /// <summary>
        /// Trockenlauf der Merkmals-Uebernahme. Andockpunkt
        /// <c>MerkmalUebernahmeCtrl.Pruefe(int, int, AbweichungsErmittler.Merkmal)</c>;
        /// Sperrspalten ueber <c>IstSchluesselspalte(string)</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// BEWUSSTE ABWEICHUNG: <c>merkmal</c> ist ein TEXT und keine Aufzaehlung, obwohl es
        /// eine feste Werteliste gibt. <c>AbweichungsErmittler.Felder</c> fuehrt 54
        /// Merkmale; als <c>enum</c> im Werkzeugkatalog waeren das rund 1,5 KB, die bei
        /// JEDER Modellanfrage mitgingen - mehr als der ganze uebrige Katalog
        /// (Fachkonzept 3.3, Kostenzeile). Der Wert wird stattdessen gegen dieselbe Liste
        /// geprueft, und die Absage nennt alle zulaessigen Schluessel; das Modell kann sich
        /// also in EINER Korrekturrunde fangen. Geraten wird nichts.
        /// </para>
        /// </remarks>
        internal static KiAktion MerkmalVorschau()
        {
            return new KiAktion(
                name: "merkmal_vorschau",
                zweck: KiAktionsTexte.ZweckMerkmalVorschau,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "MerkmalUebernahmeCtrl.Pruefe",
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlVonProjekt, name: "von_projekt",
                                             anzeigename: KiAktionsTexte.VonProjektName),
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlNachProjekt, name: "nach_projekt",
                                             anzeigename: KiAktionsTexte.NachProjektName),
                    new KiParameter("merkmal", KiParameterTyp.Text, KiAktionsTexte.ErlMerkmal,
                                    anzeigename: KiAktionsTexte.MerkmalName, maxLaenge: 120)
                },
                vorbedingung: a =>
                {
                    int von = KiHilfe.ProjektId(a, "von_projekt");
                    int nach = KiHilfe.ProjektId(a, "nach_projekt");

                    if (von == nach) return KiAktionsTexte.GleicheProjekte;

                    string grund = KiHilfe.ProjektMussAufloesbarSein(a, "von_projekt");
                    if (grund != null) return grund;
                    grund = KiHilfe.ProjektMussAufloesbarSein(a, "nach_projekt");
                    if (grund != null) return grund;

                    if (Merkmal(a.Text("merkmal")) == null)
                        return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.MerkmalUnbekannt,
                                             a.Text("merkmal"), string.Join(", ", Merkmalsschluessel()));
                    return null;
                },
                ausfuehren: a =>
                {
                    int von = KiHilfe.ProjektId(a, "von_projekt");
                    int nach = KiHilfe.ProjektId(a, "nach_projekt");
                    AbweichungsErmittler.Merkmal f = Merkmal(a.Text("merkmal"));

                    MerkmalUebernahmeCtrl.Befund b = MerkmalUebernahmeCtrl.Pruefe(von, nach, f);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "von_projekt", von,
                        "nach_projekt", nach,
                        "merkmal", Schluessel(f),
                        "gewerk", KiHilfe.Text(f.Gewerk),
                        "label", KiHilfe.Text(f.Label),
                        "einheit", KiHilfe.Text(f.Einheit),
                        "schluesselspalte", MerkmalUebernahmeCtrl.IstSchluesselspalte(f.Spalte),
                        "moeglich", b.Moeglich,
                        "gleichstand", b.Gleichstand,
                        "wert_quelle", KiHilfe.Text(b.Quelle.Anzeigewert),
                        "wert_ziel", KiHilfe.Text(b.Ziel.Anzeigewert),
                        "zeilen_quelle", b.Quelle.Anzahl,
                        "zeilen_ziel", b.Ziel.Anzahl,
                        "grund", KiHilfe.Text(b.Grund)));

                    string text;
                    if (!b.Moeglich)
                        text = string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.MerkmalNichtMoeglich, b.Grund);
                    else if (b.Gleichstand)
                        text = string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.MerkmalGleichstand,
                                             f.Label, b.Ziel.Anzeigewert);
                    else
                        text = string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.MerkmalMoeglich,
                                             f.Label, b.Ziel.Anzeigewert, b.Quelle.Anzeigewert);

                    return KiErgebnis.Ok(text, zeilen, anzahl: 1);
                });
        }

        /// <summary>Schluessel eines Merkmals: <c>Tabelle.Spalte</c>.</summary>
        internal static string Schluessel(AbweichungsErmittler.Merkmal f)
        {
            return f == null ? "" : f.Tabelle + "." + f.Spalte;
        }

        /// <summary>Sucht ein Merkmal ueber seinen Schluessel; <c>null</c>, wenn unbekannt.</summary>
        internal static AbweichungsErmittler.Merkmal Merkmal(string schluessel)
        {
            if (string.IsNullOrWhiteSpace(schluessel)) return null;
            string gesucht = schluessel.Trim();

            foreach (AbweichungsErmittler.Merkmal f in AbweichungsErmittler.Felder)
                if (string.Equals(Schluessel(f), gesucht, StringComparison.OrdinalIgnoreCase)) return f;
            return null;
        }

        /// <summary>Alle Merkmalsschluessel - Grundlage der Absage bei unbekanntem Wert.</summary>
        internal static IReadOnlyList<string> Merkmalsschluessel()
        {
            var namen = new List<string>();
            foreach (AbweichungsErmittler.Merkmal f in AbweichungsErmittler.Felder)
                namen.Add(Schluessel(f));
            return namen;
        }
    }
}
