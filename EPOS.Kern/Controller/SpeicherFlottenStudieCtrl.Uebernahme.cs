using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1;

/// <summary>
/// EINE angelegte oder geänderte Speicheranlage der Übernahme (Auftrag #247).
/// </summary>
/// <param name="EinheitId">Kennung der Flotteneinheit, aus der sie entstanden ist.</param>
/// <param name="AnlageId"><c>Tab_Energieanlagen.ID</c> der Anlage.</param>
/// <param name="GeraeteId"><c>Tab_Stromspeicher.ID</c> des Gerätedatensatzes.</param>
/// <param name="Bezeichner">Der vergebene Bezeichner.</param>
/// <param name="Neu"><c>true</c> = angelegt, <c>false</c> = eine vorhandene Anlage geändert.</param>
/// <param name="Stueck">Die laufende Nummer des Stücks (1-basiert).</param>
public sealed record FlottenUebernahmeAnlage(string EinheitId, int AnlageId, int GeraeteId,
                                             string Bezeichner, bool Neu, int Stueck);

/// <summary>
/// Wie viele Anlagen eine Übernahme ANLEGEN und wie viele sie ÄNDERN würde — die Zahlen
/// der Rückfrage („n Speicheranlagen werden angelegt, m geändert — fortfahren?").
/// </summary>
/// <param name="Angelegt">Zahl der neu entstehenden Anlagen.</param>
/// <param name="Geaendert">Zahl der zurückgeschriebenen Anlagen.</param>
public readonly record struct FlottenUebernahmeVorschau(int Angelegt, int Geaendert)
{
    /// <summary>Gibt es überhaupt etwas zu tun?</summary>
    public bool Leer => Angelegt == 0 && Geaendert == 0;
}

/// <summary>Das Ergebnis einer Übernahme.</summary>
/// <param name="Erfolg">Alles geschrieben und festgeschrieben?</param>
/// <param name="Meldung">Der Grund eines Fehlschlags; bei Erfolg leer.</param>
/// <param name="Anlagen">Die angelegten und geänderten Anlagen samt ihren Kennungen.</param>
public sealed record FlottenUebernahmeErgebnis(bool Erfolg, string Meldung,
                                               IReadOnlyList<FlottenUebernahmeAnlage> Anlagen)
{
    /// <summary>Zahl der neu angelegten Anlagen.</summary>
    public int Angelegt => Anlagen.Count(a => a.Neu);

    /// <summary>Zahl der geänderten Anlagen.</summary>
    public int Geaendert => Anlagen.Count(a => !a.Neu);

    /// <summary>Der Fehlerfall mit Grund.</summary>
    public static FlottenUebernahmeErgebnis Fehler(string grund)
        => new(false, grund ?? "", Array.Empty<FlottenUebernahmeAnlage>());
}

/// <summary>
/// <b>„Ausgewählte Einheiten in Projekt übernehmen"</b> (Auftrag #247, Anwenderentscheid
/// SD‑E‑10 / SD‑Q15, Konzept „Stromspeicher-Dialoge" 8.3).
///
/// <para><b>Warum es diesen Weg gibt.</b> Der Anwender hat entschieden, dass die
/// Flotteneinheiten KEINE Herkunftsmarkierung tragen: <i>„die Einheiten werden nur
/// temporär für die Optimierung benötigt"</i>. Damit aus einer gefundenen Bestückung
/// trotzdem ein Projekt werden kann, bekommt Schritt 1 den Knopf, der aus den gewählten
/// Einheiten Speicheranlagen des Projekts macht.</para>
///
/// <para><b>Je Stück EINE Anlage.</b> <c>Tab_Stromspeicher</c> kennt keine Stückzahl; eine
/// n-fach große Anlage wäre eine ANDERE Aussage als n Geräte (Konzept 8.5, von der
/// Orchestrierung ohne Rückfrage entschieden). Bei mehreren Stück trägt der Bezeichner
/// eine laufende Nummer.</para>
///
/// <para><b>Rückschreiben statt Dublette.</b> Vertritt die Einheit bereits eine
/// Projektanlage (<see cref="FlottenEinheit.AnlageId"/>), gehen ihre Werte in DIESE
/// Anlage; weitere Stück entstehen daneben als neue Anlagen.</para>
///
/// <para><b>Alles in EINER Transaktion</b> (<c>DataRepository.Vorgang()</c>): Eine halb
/// übernommene Flotte — Gerät ohne Anlagenzeile — wäre der Zustand, den niemand mehr
/// auseinanderhält. Der gespeicherte AUSLEGUNGSSTAND bleibt unberührt: Der Projektlauf
/// rechnet weiterhin den aktivierten Stand <c>@Projektflotte</c> (SP‑O‑8).</para>
/// </summary>
public static partial class SpeicherFlottenStudieCtrl
{
    /// <summary>
    /// Die Umkehrung von <c>SpeicherFlottenStudieCtrl.EinheitAusKatalog</c> /
    /// <c>StromspeicherSimCtrl.LeseParameter</c>: die Gerätespalten EINER Einheit.
    /// </summary>
    /// <remarks>
    /// <para>Geschrieben wird genau das, was die Leserichtung kennt:
    /// <c>Energie</c> ← Kapazität, <c>Leistung</c> ← die GRÖSSERE der zwei
    /// Richtungsleistungen (die Anlage trägt EINEN Wert), <c>Wirkungsgrad_RT</c> ←
    /// <c>eta_lade · eta_entlade</c> (die Leserichtung zieht die Wurzel),
    /// <c>Ladezustand</c> ← Start-SoC in Prozent, <c>Modulkosten</c>/<c>Leistungskosten</c>/
    /// <c>Investition_Fix</c> ← die drei Investitionssätze, <c>Standby_Verbrauch</c> ←
    /// Hilfsverbrauch in W.</para>
    /// <para><b>Was NICHT geschrieben wird</b>, ist die Gegenseite dessen, was
    /// <c>EinheitAusKatalog</c> nicht liest: <c>Typ</c>, <c>Firma</c>, <c>Degradation</c>,
    /// <c>Zyklen_Zugesichert</c> und <c>Verschleisskosten</c> haben in
    /// <see cref="FlottenEinheit"/> kein Gegenstück. Beim RÜCKSCHREIBEN in eine vorhandene
    /// Anlage bleiben sie deshalb stehen, statt von Nullen überschrieben zu werden.</para>
    /// </remarks>
    private const string SQL_GERAET_NEU = @"INSERT INTO Tab_Stromspeicher
        (ID, ID_Projekt, Bezeichner, Energie, Leistung, Ladezustand, Modulkosten,
         Wirkungsgrad_RT, Leistungskosten, Investition_Fix, Standby_Verbrauch)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

    private const string SQL_GERAET_AENDERN = @"UPDATE Tab_Stromspeicher
        SET Energie = ?, Leistung = ?, Ladezustand = ?, Modulkosten = ?,
            Wirkungsgrad_RT = ?, Leistungskosten = ?, Investition_Fix = ?, Standby_Verbrauch = ?
        WHERE ID = ?";

    /// <summary>
    /// Wie viele Anlagen die Übernahme anlegen und wie viele sie ändern würde — OHNE
    /// einen einzigen Schreibzugriff.
    /// </summary>
    /// <remarks>
    /// Reine Arithmetik: Eine Einheit mit Anlagenbezug ändert ihre Anlage und legt
    /// <c>n − 1</c> weitere an; eine ohne legt <c>n</c> an. Eine Stückzahl unter 1 gilt
    /// als 1 — wer eine Einheit übernimmt, will mindestens ein Gerät.
    /// </remarks>
    /// <param name="einheiten">Die gewählten Einheiten.</param>
    /// <param name="stueckzahlen">Die Stückzahl je Einheit, in derselben Reihenfolge; <c>null</c> = je 1.</param>
    public static FlottenUebernahmeVorschau Vorschau(IReadOnlyList<FlottenEinheit> einheiten,
                                                     IReadOnlyList<int> stueckzahlen = null)
    {
        int angelegt = 0, geaendert = 0;
        for (int i = 0; i < (einheiten?.Count ?? 0); i++)
        {
            FlottenEinheit e = einheiten[i];
            if (e == null) continue;
            int stueck = Stueckzahl(stueckzahlen, i);
            bool vertritt = !string.IsNullOrWhiteSpace(e.AnlageId);
            if (vertritt) { geaendert++; angelegt += stueck - 1; }
            else angelegt += stueck;
        }
        return new FlottenUebernahmeVorschau(angelegt, geaendert);
    }

    /// <summary>
    /// Macht aus den gewählten Einheiten Speicheranlagen des Projekts — je Stück eine,
    /// alles in EINER Transaktion.
    /// </summary>
    /// <param name="projektId">Das offene Projekt.</param>
    /// <param name="einheiten">Die gewählten Einheiten.</param>
    /// <param name="stueckzahlen">Die Stückzahl je Einheit, in derselben Reihenfolge; <c>null</c> = je 1.</param>
    /// <returns>
    /// Die angelegten und geänderten Anlagen samt ihren Kennungen — daran setzt die
    /// Oberfläche die <see cref="FlottenEinheit.AnlageId"/> der Einheiten.
    /// </returns>
    public static FlottenUebernahmeErgebnis EinheitenInProjektUebernehmen(
        int projektId,
        IReadOnlyList<FlottenEinheit> einheiten,
        IReadOnlyList<int> stueckzahlen = null)
    {
        if (projektId <= 0)
            return FlottenUebernahmeErgebnis.Fehler(MyResource.Resource.FLOTTE_UEBERNAHME_KEIN_PROJEKT);
        if (einheiten == null || einheiten.Count == 0)
            return FlottenUebernahmeErgebnis.Fehler(MyResource.Resource.FLOTTE_UEBERNAHME_KEINE_EINHEIT);

        // Die sechs Geraetespalten VOR der Transaktion sicherstellen: Sie stehen
        // namentlich im INSERT, und ein DDL mitten in einem offenen Vorgang waere der
        // zweite Schreibweg auf dieselbe Datei (Muster StromspeicherCtrl.CopyFromStamm).
        StromspeicherCtrl.StelleGeraetespaltenSicher();

        var angelegt = new List<FlottenUebernahmeAnlage>();
        DbVorgang v = null;
        try
        {
            v = DataRepository.Vorgang();
            var vergeben = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < einheiten.Count; i++)
            {
                FlottenEinheit e = einheiten[i];
                if (e == null) continue;
                int stueck = Stueckzahl(stueckzahlen, i);
                int vorhandeneAnlage = Anlagenbezug(e);

                for (int n = 1; n <= stueck; n++)
                {
                    // DAS ERSTE STUECK schreibt in die vertretene Anlage zurueck; jedes
                    // weitere entsteht neu (Konzept 8.3).
                    if (n == 1 && vorhandeneAnlage > 0 &&
                        Geraetezeile(v, projektId, vorhandeneAnlage) is { } geraet && geraet > 0)
                    {
                        v.Ausfuehren(SQL_GERAET_AENDERN, Geraetewerte(e, geraet));
                        angelegt.Add(new FlottenUebernahmeAnlage(e.Id ?? "", vorhandeneAnlage, geraet,
                            Bezeichner(v, projektId, geraet), false, n));
                        continue;
                    }

                    string name = FreierBezeichner(v, projektId, Grundname(e, n, stueck), vergeben);
                    int neueGeraeteId = NaechsteGeraeteId(v);
                    v.Ausfuehren(SQL_GERAET_NEU, Geraeteanlage(e, neueGeraeteId, projektId, name));

                    var zeile = new WErzeugerModel
                    {
                        ID_Projekt = projektId,
                        Bezeichner = name,
                        ID_Type = WizardItemClass.SP_TYP,
                        ID_SP = neueGeraeteId
                    };
                    int neueAnlageId = v.EinfuegenUndId(AnlagenSql.SQL_ANLAGE_INSERT,
                                                        AnlagenSql.AnlagenParameter(projektId, zeile));
                    angelegt.Add(new FlottenUebernahmeAnlage(e.Id ?? "", neueAnlageId, neueGeraeteId,
                                                             name, true, n));
                }
            }

            v.Commit();
        }
        catch (Exception ex)
        {
            if (v != null) { try { v.Rollback(); } catch { } }
            return FlottenUebernahmeErgebnis.Fehler(string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.FLOTTE_UEBERNAHME_FEHLER, ex.Message));
        }
        finally
        {
            if (v != null) { try { v.Dispose(); } catch { } }
        }

        // NACH dem Commit: Das Projekt hat jetzt eine Anlage der elektrischen Welt und
        // braucht seinen Stromtraeger (ET-2) — derselbe Nachzug wie in WErzeugerCtrl.
        if (angelegt.Any(a => a.Neu))
            try { ProjektEnergietraegerCtrl.StromTraegerSicherstellen(projektId); }
            catch { }

        return new FlottenUebernahmeErgebnis(true, "", angelegt);
    }

    // =====================================================================
    //  Innenleben
    // =====================================================================

    private static int Stueckzahl(IReadOnlyList<int> stueckzahlen, int stelle)
    {
        int n = stueckzahlen != null && stelle < stueckzahlen.Count ? stueckzahlen[stelle] : 1;
        return n < 1 ? 1 : n;
    }

    /// <summary>Die Anlagen-Id einer Einheit; 0 = sie vertritt keine Projektanlage.</summary>
    private static int Anlagenbezug(FlottenEinheit e)
        => int.TryParse(e.AnlageId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) && id > 0
            ? id : 0;

    /// <summary>
    /// Die Gerätezeile einer Speicheranlage dieses Projekts (<c>Tab_Energieanlagen.ID_SP</c>);
    /// 0, wenn die Anlage nicht (mehr) zum Projekt gehört oder kein Gerät führt.
    /// </summary>
    private static int Geraetezeile(DbVorgang v, int projektId, int anlageId)
    {
        object o = v.Skalar("SELECT ID_SP FROM Tab_Energieanlagen WHERE ID = ? AND ID_Projekt = ?",
                            new DbParam("@anl", anlageId), new DbParam("@proj", projektId));
        return o == null ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
    }

    private static string Bezeichner(DbVorgang v, int projektId, int geraeteId)
    {
        object o = v.Skalar("SELECT Bezeichner FROM Tab_Stromspeicher WHERE ID = ? AND ID_Projekt = ?",
                            new DbParam("@id", geraeteId), new DbParam("@proj", projektId));
        return o?.ToString() ?? "";
    }

    private static int NaechsteGeraeteId(DbVorgang v)
    {
        object o = v.Skalar("SELECT MAX(ID) FROM Tab_Stromspeicher");
        return (o == null ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture)) + 1;
    }

    /// <summary>Der Grundname eines Stücks: bei mehreren mit laufender Nummer.</summary>
    private static string Grundname(FlottenEinheit e, int stueck, int gesamt)
    {
        string name = string.IsNullOrWhiteSpace(e.Name) ? MyResource.Resource.FLOTTE_ED_UNBENANNT : e.Name.Trim();
        return gesamt <= 1 ? name
            : name + " " + stueck.ToString(CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Ein im Projekt noch freier Bezeichner — geprüft IN der Transaktion und gegen die
    /// in diesem Lauf bereits vergebenen Namen.
    /// </summary>
    /// <remarks>
    /// <c>AnlagenEindeutigkeit.EindeutigerBezeichner</c> fragt über <c>StilleDb</c> und
    /// damit auf einer ZWEITEN Verbindung; die sähe die eben eingefügten Zeilen nicht.
    /// Dieselbe Regel („Name (2)"), nur im Vorgang.
    /// </remarks>
    private static string FreierBezeichner(DbVorgang v, int projektId, string wunsch,
                                            HashSet<string> vergeben)
    {
        string basis = string.IsNullOrWhiteSpace(wunsch) ? MyResource.Resource.FLOTTE_ED_UNBENANNT : wunsch.Trim();
        for (int n = 1; n < 1000; n++)
        {
            string kandidat = n == 1 ? basis
                : basis + " (" + n.ToString(CultureInfo.CurrentCulture) + ")";
            if (vergeben.Contains(kandidat)) continue;

            object o = v.Skalar("SELECT COUNT(*) FROM Tab_Stromspeicher WHERE ID_Projekt = ? AND Bezeichner = ?",
                                new DbParam("@proj", projektId), new DbParam("@bez", kandidat));
            if (Convert.ToInt32(o ?? 0, CultureInfo.InvariantCulture) == 0)
            {
                vergeben.Add(kandidat);
                return kandidat;
            }
        }
        string notnagel = basis + " (" + DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture) + ")";
        vergeben.Add(notnagel);
        return notnagel;
    }

    /// <summary>Die acht Gerätewerte samt der Ziel-Id — für das UPDATE.</summary>
    private static DbParam[] Geraetewerte(FlottenEinheit e, int geraeteId) => new[]
    {
        new DbParam("@ene", Energie(e)),
        new DbParam("@lei", Leistung(e)),
        new DbParam("@lad", Ladezustand(e)),
        new DbParam("@mod", e.InvestitionEuroProKWh),
        new DbParam("@eta", Wirkungsgrad(e)),
        new DbParam("@cpow", e.InvestitionEuroProKw),
        new DbParam("@ifix", e.InvestitionEuro),
        new DbParam("@stby", e.HilfsverbrauchKw * 1000.0),
        new DbParam("@id", geraeteId)
    };

    /// <summary>Dieselben Werte samt Id, Projekt und Bezeichner — für das INSERT.</summary>
    private static DbParam[] Geraeteanlage(FlottenEinheit e, int geraeteId, int projektId, string name) => new[]
    {
        new DbParam("@id", geraeteId),
        new DbParam("@proj", projektId),
        new DbParam("@bez", name ?? ""),
        new DbParam("@ene", Energie(e)),
        new DbParam("@lei", Leistung(e)),
        new DbParam("@lad", Ladezustand(e)),
        new DbParam("@mod", e.InvestitionEuroProKWh),
        new DbParam("@eta", Wirkungsgrad(e)),
        new DbParam("@cpow", e.InvestitionEuroProKw),
        new DbParam("@ifix", e.InvestitionEuro),
        new DbParam("@stby", e.HilfsverbrauchKw * 1000.0)
    };

    private static double Energie(FlottenEinheit e) => Endlich(e.KapazitaetKWh);

    /// <summary>Die Anlage trägt EINEN Leistungswert; genommen wird die größere Richtung.</summary>
    private static double Leistung(FlottenEinheit e)
        => Endlich(Math.Max(e.LadeleistungKw, e.EntladeleistungKw));

    /// <summary>Der Start-SoC in Prozent — die Leserichtung teilt durch 100.</summary>
    private static double Ladezustand(FlottenEinheit e) => Endlich(e.SocStart * 100.0);

    /// <summary>
    /// Der Umlaufwirkungsgrad: <c>eta_lade · eta_entlade</c>. Die Leserichtung
    /// (<c>SpeicherParameter</c>) zieht daraus die Wurzel und verteilt sie auf beide
    /// Richtungen — bei getrennten Richtungswirkungsgraden ist das der Preis dafür, dass
    /// die Projekttabelle nur EINEN Wert kennt.
    /// </summary>
    private static double Wirkungsgrad(FlottenEinheit e)
    {
        double eta = e.Ladewirkungsgrad * e.Entladewirkungsgrad;
        return eta > 0.0 && eta <= 1.0 ? eta : 0.0;
    }

    private static double Endlich(double wert) => double.IsFinite(wert) && wert > 0.0 ? wert : 0.0;
}
