using System.Collections.Generic;
using System.Data;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests;

/// <summary>
/// Ein ZÄHLENDER Mantel um die Zugriffsschicht (Auftrag #254): Er reicht jeden Aufruf
/// unverändert durch und notiert nur, wie viele es waren.
/// </summary>
/// <remarks>
/// <para><b>Wozu.</b> „Diese Stelle fragt die Datenbank nicht mehr" ist eine Aussage
/// über ZÄHLBARES, nicht über Millisekunden. Der Mantel macht sie prüfbar:
/// <c>DataRepository.Zugriff</c> ist ein internes Feld, das ein Fall für seine Dauer
/// tauschen darf — dasselbe Muster, mit dem <c>TestDatenbank</c> den Pfad umbiegt.</para>
/// <para><b>Wer ihn einlegt, legt ihn auch zurück</b> — in einem <c>finally</c>. Die
/// Sammlung „Testdatenbank" läuft seriell, aber das Feld ist prozessweit.</para>
/// </remarks>
internal sealed class Zaehlzugriff : IDatenzugriff
{
    /// <summary>Legt den Mantel um einen bestehenden Zugriff.</summary>
    /// <param name="innen">Der echte Zugriff; er bekommt jeden Aufruf.</param>
    public Zaehlzugriff(IDatenzugriff innen) => Innen = innen;

    /// <summary>Der eingewickelte Zugriff — ihn legt der Fall danach zurück.</summary>
    public IDatenzugriff Innen { get; }

    /// <summary>Die Zahl der bisher durchgereichten Vorgänge.</summary>
    public int Gesamt { get; private set; }

    /// <summary>Setzt den Zähler zurück.</summary>
    public void Nullen() => Gesamt = 0;

    private void Zaehle() => Gesamt++;

    /// <inheritdoc />
    public DataTable GetDataTable(string sql, params DbParam[] parameter)
    { Zaehle(); return Innen.GetDataTable(sql, parameter); }

    /// <inheritdoc />
    public bool ExecuteSQL(string sql, params DbParam[] parameter)
    { Zaehle(); return Innen.ExecuteSQL(sql, parameter); }

    /// <inheritdoc />
    public int ExecuteNonQuery(string sql, params DbParam[] parameter)
    { Zaehle(); return Innen.ExecuteNonQuery(sql, parameter); }

    /// <inheritdoc />
    public int ExecuteInsertAndGetId(string insertSql, DbParam[] parameter)
    { Zaehle(); return Innen.ExecuteInsertAndGetId(insertSql, parameter); }

    /// <inheritdoc />
    public object ExecuteScalar(string sql, params DbParam[] parameter)
    { Zaehle(); return Innen.ExecuteScalar(sql, parameter); }

    /// <inheritdoc />
    public DbVorgang Vorgang()
    { Zaehle(); return Innen.Vorgang(); }

    /// <inheritdoc />
    public bool TabelleVorhanden(string name)
    { Zaehle(); return Innen.TabelleVorhanden(name); }

    /// <inheritdoc />
    public bool SpalteVorhanden(string tabelle, string spalte)
    { Zaehle(); return Innen.SpalteVorhanden(tabelle, spalte); }

    /// <inheritdoc />
    public List<string> SpaltenVonTabelle(string tabelle)
    { Zaehle(); return Innen.SpaltenVonTabelle(tabelle); }

    /// <inheritdoc />
    public DataTable IndexListe(string tabelle)
    { Zaehle(); return Innen.IndexListe(tabelle); }

    /// <inheritdoc />
    public DataTable FremdschluesselListe(string tabelle)
    { Zaehle(); return Innen.FremdschluesselListe(tabelle); }

    /// <summary>Die Umgebungsfrage zählt nicht mit — sie ist kein Vorgang auf den Daten.</summary>
    public bool DatenbankVorhanden() => Innen.DatenbankVorhanden();

    /// <inheritdoc />
    public string DatenbankPfad => Innen.DatenbankPfad;
}
