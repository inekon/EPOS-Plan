Option Compare Database
Option Explicit

' ===================================================================================
'  CreateErgebnisTabellen
'  Legt die Ergebnistabellen fuer die Simulationsergebnisse an:
'
'    Tab_Ergebnis                  (Kopf, 1 Zeile je Lauf/Projekt)
'    Tab_ErgebnisEnergiebedarf     (Waerme-/Strombedarf, 1:1 zum Kopf)
'    Tab_ErgebnisWaermepumpe       (WP-Aggregat, 1:1 zum Kopf)
'    Tab_ErgebnisWaermepumpeModul  (Modulauflistung der WP, 1:n zur WP-Zeile)
'
'  Verknuepfungen mit Loeschweitergabe (Cascade):
'    Tab_Ergebnis (ID)            -> Tab_ErgebnisEnergiebedarf (ID_Ergebnis)
'    Tab_Ergebnis (ID)            -> Tab_ErgebnisWaermepumpe   (ID_Ergebnis)
'    Tab_ErgebnisWaermepumpe (ID) -> Tab_ErgebnisWaermepumpeModul (ID_ErgebnisWaermepumpe)
'
'  Das Makro ist wiederholbar: vorhandene Beziehungen/Tabellen werden zuvor entfernt
'  (Ergebnisse sind aus der Simulation jederzeit neu erzeugbar).
'  Weitere Simulationsarten (Heizkessel, Solarthermie, BHKW, PV, Stromspeicher)
'  koennen spaeter nach demselben Muster ergaenzt werden.
' ===================================================================================

Public Sub CreateErgebnisTabellen()
    Dim db As DAO.Database
    Set db = CurrentDb

    ' 1) Alte Beziehungen entfernen (falls vorhanden)
    DropRelationIfExists db, "Rel_Ergebnis_Energiebedarf"
    DropRelationIfExists db, "Rel_Ergebnis_Waermepumpe"
    DropRelationIfExists db, "Rel_Waermepumpe_Modul"
    DropRelationIfExists db, "Rel_Ergebnis_BHKW"
    DropRelationIfExists db, "Rel_BHKW_Modul"
    DropRelationIfExists db, "Rel_Ergebnis_Heizkessel"
    DropRelationIfExists db, "Rel_Heizkessel_Modul"

    ' 2) Alte Tabellen entfernen (Kinder zuerst)
    DropTableIfExists db, "Tab_ErgebnisWaermepumpeModul"
    DropTableIfExists db, "Tab_ErgebnisWaermepumpe"
    DropTableIfExists db, "Tab_ErgebnisBHKWModul"
    DropTableIfExists db, "Tab_ErgebnisBHKW"
    DropTableIfExists db, "Tab_ErgebnisHeizkesselModul"
    DropTableIfExists db, "Tab_ErgebnisHeizkessel"
    DropTableIfExists db, "Tab_ErgebnisEnergiebedarf"
    DropTableIfExists db, "Tab_Ergebnis"

    ' Optional: fruehere generische Ergebnistabellen aufraeumen (aus altem Entwurf)
    DropTableIfExists db, "Tab_ErgebnisKomponente"
    DropTableIfExists db, "Tab_ErgebnisMonat"

    ' 3) Tabellen neu anlegen
    CreateKopf db
    CreateEnergiebedarf db
    CreateWaermepumpe db
    CreateWaermepumpeModul db
    CreateBHKW db
    CreateBHKWModul db
    CreateHeizkessel db
    CreateHeizkesselModul db

    ' 4) Beziehungen mit Loeschweitergabe anlegen
    db.Relations.Refresh
    CreateCascade db, "Rel_Ergebnis_Energiebedarf", "Tab_Ergebnis", "Tab_ErgebnisEnergiebedarf", "ID", "ID_Ergebnis"
    CreateCascade db, "Rel_Ergebnis_Waermepumpe", "Tab_Ergebnis", "Tab_ErgebnisWaermepumpe", "ID", "ID_Ergebnis"
    CreateCascade db, "Rel_Waermepumpe_Modul", "Tab_ErgebnisWaermepumpe", "Tab_ErgebnisWaermepumpeModul", "ID", "ID_ErgebnisWaermepumpe"
    CreateCascade db, "Rel_Ergebnis_BHKW", "Tab_Ergebnis", "Tab_ErgebnisBHKW", "ID", "ID_Ergebnis"
    CreateCascade db, "Rel_BHKW_Modul", "Tab_ErgebnisBHKW", "Tab_ErgebnisBHKWModul", "ID", "ID_ErgebnisBHKW"
    CreateCascade db, "Rel_Ergebnis_Heizkessel", "Tab_Ergebnis", "Tab_ErgebnisHeizkessel", "ID", "ID_Ergebnis"
    CreateCascade db, "Rel_Heizkessel_Modul", "Tab_ErgebnisHeizkessel", "Tab_ErgebnisHeizkesselModul", "ID", "ID_ErgebnisHeizkessel"

    db.TableDefs.Refresh
    db.Relations.Refresh

    MsgBox "Ergebnistabellen wurden angelegt.", vbInformation
End Sub

' -----------------------------------------------------------------------------------
' Tab_Ergebnis (Kopf)
' -----------------------------------------------------------------------------------
Private Sub CreateKopf(db As DAO.Database)
    Dim td As DAO.TableDef
    Set td = db.CreateTableDef("Tab_Ergebnis")

    td.Fields.Append td.CreateField("ID", dbLong)
    td.Fields.Append td.CreateField("ID_Projekt", dbLong)
    td.Fields.Append FldText(td, "Bezeichner", 255)
    td.Fields.Append td.CreateField("Zeitstempel", dbDate)
    td.Fields.Append td.CreateField("ID_Klimaregion", dbLong)
    td.Fields.Append td.CreateField("Sim_Energiebedarf", dbBoolean)
    td.Fields.Append td.CreateField("Sim_Waermepumpe", dbBoolean)
    td.Fields.Append td.CreateField("Sim_Heizkessel", dbBoolean)
    td.Fields.Append td.CreateField("Sim_Solarthermie", dbBoolean)
    td.Fields.Append td.CreateField("Sim_BHKW", dbBoolean)
    td.Fields.Append td.CreateField("Sim_PV", dbBoolean)
    td.Fields.Append td.CreateField("Sim_Stromspeicher", dbBoolean)

    db.TableDefs.Append td
    AddPrimaryKey db, "Tab_Ergebnis", "ID"
End Sub

' -----------------------------------------------------------------------------------
' Tab_ErgebnisEnergiebedarf
' -----------------------------------------------------------------------------------
Private Sub CreateEnergiebedarf(db As DAO.Database)
    Dim td As DAO.TableDef
    Set td = db.CreateTableDef("Tab_ErgebnisEnergiebedarf")

    td.Fields.Append td.CreateField("ID", dbLong)
    td.Fields.Append td.CreateField("ID_Ergebnis", dbLong)
    td.Fields.Append td.CreateField("Waermebedarf_Gesamt", dbDouble)   ' MWh
    td.Fields.Append td.CreateField("Waermelast_Max", dbDouble)        ' kW
    td.Fields.Append td.CreateField("Strombedarf_Gesamt", dbDouble)    ' MWh
    td.Fields.Append td.CreateField("Strombedarf_Max", dbDouble)       ' kW

    db.TableDefs.Append td
    AddPrimaryKey db, "Tab_ErgebnisEnergiebedarf", "ID"
End Sub

' -----------------------------------------------------------------------------------
' Tab_ErgebnisWaermepumpe (Aggregat)
' -----------------------------------------------------------------------------------
Private Sub CreateWaermepumpe(db As DAO.Database)
    Dim td As DAO.TableDef
    Set td = db.CreateTableDef("Tab_ErgebnisWaermepumpe")

    td.Fields.Append td.CreateField("ID", dbLong)
    td.Fields.Append td.CreateField("ID_Ergebnis", dbLong)
    td.Fields.Append td.CreateField("Waermebedarf", dbDouble)                ' MWh/a
    td.Fields.Append td.CreateField("Restwaermebedarf", dbDouble)            ' MWh/a
    td.Fields.Append td.CreateField("Waermeproduktion_WP", dbDouble)         ' MWh/a
    td.Fields.Append td.CreateField("Stromverbrauch_WP", dbDouble)           ' MWh/a
    td.Fields.Append td.CreateField("Stromverbrauch_Heizstab", dbDouble)     ' MWh/a
    td.Fields.Append td.CreateField("Kapazitaet_Pufferspeicher", dbDouble)   ' kWh
    td.Fields.Append td.CreateField("Min_Spitzenkesselleistung", dbDouble)   ' kW
    td.Fields.Append td.CreateField("Waermebedarfsdeckung", dbDouble)        ' %
    td.Fields.Append td.CreateField("Vollbenutzungsstunden", dbDouble)       ' h/a
    td.Fields.Append td.CreateField("Bivalenzpunkt", dbDouble)              ' Grad C (kann leer sein)

    db.TableDefs.Append td
    AddPrimaryKey db, "Tab_ErgebnisWaermepumpe", "ID"
End Sub

' -----------------------------------------------------------------------------------
' Tab_ErgebnisWaermepumpeModul (Modulauflistung)
' -----------------------------------------------------------------------------------
Private Sub CreateWaermepumpeModul(db As DAO.Database)
    Dim td As DAO.TableDef
    Set td = db.CreateTableDef("Tab_ErgebnisWaermepumpeModul")

    td.Fields.Append td.CreateField("ID", dbLong)
    td.Fields.Append td.CreateField("ID_ErgebnisWaermepumpe", dbLong)
    td.Fields.Append FldText(td, "Modul", 255)
    td.Fields.Append td.CreateField("Leistung", dbDouble)          ' kW
    td.Fields.Append td.CreateField("Waermeproduktion", dbDouble)  ' MWh/a
    td.Fields.Append td.CreateField("Stromverbrauch", dbDouble)    ' MWh/a
    td.Fields.Append td.CreateField("Heizstab", dbDouble)          ' MWh/a
    td.Fields.Append td.CreateField("Betriebsstunden", dbDouble)   ' h/a

    db.TableDefs.Append td
    AddPrimaryKey db, "Tab_ErgebnisWaermepumpeModul", "ID"
End Sub

' -----------------------------------------------------------------------------------
' Tab_ErgebnisBHKW (Aggregat)
' -----------------------------------------------------------------------------------
Private Sub CreateBHKW(db As DAO.Database)
    Dim td As DAO.TableDef
    Set td = db.CreateTableDef("Tab_ErgebnisBHKW")

    td.Fields.Append td.CreateField("ID", dbLong)
    td.Fields.Append td.CreateField("ID_Ergebnis", dbLong)
    td.Fields.Append td.CreateField("Waermebedarf", dbDouble)                 ' MWh/a
    td.Fields.Append td.CreateField("Restwaermebedarf", dbDouble)             ' MWh/a
    td.Fields.Append td.CreateField("Strombedarf", dbDouble)                  ' MWh/a
    td.Fields.Append td.CreateField("Reststrombedarf", dbDouble)              ' MWh/a
    td.Fields.Append td.CreateField("Waermeproduktion", dbDouble)             ' MWh/a
    td.Fields.Append td.CreateField("Waermeueberschuss", dbDouble)            ' MWh/a
    td.Fields.Append td.CreateField("Stromproduktion", dbDouble)              ' MWh/a
    td.Fields.Append td.CreateField("Betriebsstunden_Gesamt", dbDouble)       ' h/a
    td.Fields.Append td.CreateField("Betriebsstunden_Durchschnitt", dbDouble) ' h/a
    td.Fields.Append td.CreateField("Waermebedarfsdeckung", dbDouble)         ' %
    td.Fields.Append td.CreateField("Strombedarfsdeckung", dbDouble)          ' %
    td.Fields.Append td.CreateField("Gasverbrauch_Hu", dbDouble)              ' MWh/a

    db.TableDefs.Append td
    AddPrimaryKey db, "Tab_ErgebnisBHKW", "ID"
End Sub

' -----------------------------------------------------------------------------------
' Tab_ErgebnisBHKWModul (Modulauflistung)
' -----------------------------------------------------------------------------------
Private Sub CreateBHKWModul(db As DAO.Database)
    Dim td As DAO.TableDef
    Set td = db.CreateTableDef("Tab_ErgebnisBHKWModul")

    td.Fields.Append td.CreateField("ID", dbLong)
    td.Fields.Append td.CreateField("ID_ErgebnisBHKW", dbLong)
    td.Fields.Append FldText(td, "Modul", 255)
    td.Fields.Append td.CreateField("Waermeproduktion", dbDouble)  ' MWh/a
    td.Fields.Append td.CreateField("Stromproduktion", dbDouble)   ' MWh/a

    db.TableDefs.Append td
    AddPrimaryKey db, "Tab_ErgebnisBHKWModul", "ID"
End Sub

' -----------------------------------------------------------------------------------
' Tab_ErgebnisHeizkessel (Aggregat)
' -----------------------------------------------------------------------------------
Private Sub CreateHeizkessel(db As DAO.Database)
    Dim td As DAO.TableDef
    Set td = db.CreateTableDef("Tab_ErgebnisHeizkessel")

    td.Fields.Append td.CreateField("ID", dbLong)
    td.Fields.Append td.CreateField("ID_Ergebnis", dbLong)
    td.Fields.Append td.CreateField("Waermebedarf", dbDouble)             ' MWh/a
    td.Fields.Append td.CreateField("Restwaermebedarf", dbDouble)         ' MWh/a
    td.Fields.Append td.CreateField("Waermeproduktion", dbDouble)         ' MWh/a
    td.Fields.Append td.CreateField("Strombedarf", dbDouble)              ' MWh/a
    td.Fields.Append td.CreateField("Reststrombedarf", dbDouble)          ' MWh/a
    td.Fields.Append td.CreateField("Waermebedarfsdeckung", dbDouble)     ' %
    td.Fields.Append td.CreateField("Stromverbrauch", dbDouble)           ' MWh/a
    td.Fields.Append td.CreateField("Maximale_Kesselleistung", dbDouble)  ' kW
    td.Fields.Append td.CreateField("Gasspitze", dbDouble)                ' kW
    td.Fields.Append td.CreateField("Gasverbrauch", dbDouble)             ' MWh/a
    td.Fields.Append td.CreateField("Oelverbrauch", dbDouble)             ' MWh/a
    td.Fields.Append td.CreateField("Koks", dbDouble)                     ' MWh/a
    td.Fields.Append td.CreateField("Rapsoelverbrauch", dbDouble)         ' MWh/a
    td.Fields.Append td.CreateField("Holzverbrauch", dbDouble)            ' MWh/a
    td.Fields.Append td.CreateField("Kohle", dbDouble)                    ' MWh/a
    td.Fields.Append td.CreateField("Sonstigverbrauch", dbDouble)         ' MWh/a
    td.Fields.Append td.CreateField("Pellets", dbDouble)                  ' MWh/a
    td.Fields.Append td.CreateField("TierischeFette", dbDouble)           ' MWh/a

    db.TableDefs.Append td
    AddPrimaryKey db, "Tab_ErgebnisHeizkessel", "ID"
End Sub

' -----------------------------------------------------------------------------------
' Tab_ErgebnisHeizkesselModul (Modulauflistung)
' -----------------------------------------------------------------------------------
Private Sub CreateHeizkesselModul(db As DAO.Database)
    Dim td As DAO.TableDef
    Set td = db.CreateTableDef("Tab_ErgebnisHeizkesselModul")

    td.Fields.Append td.CreateField("ID", dbLong)
    td.Fields.Append td.CreateField("ID_ErgebnisHeizkessel", dbLong)
    td.Fields.Append FldText(td, "Modul", 255)
    td.Fields.Append td.CreateField("Waerme_Gas", dbDouble)          ' MWh/a
    td.Fields.Append td.CreateField("Waerme_Oel", dbDouble)          ' MWh/a
    td.Fields.Append td.CreateField("Jahresnutzungsgrad", dbDouble)  ' %

    db.TableDefs.Append td
    AddPrimaryKey db, "Tab_ErgebnisHeizkesselModul", "ID"
End Sub

' ===================================================================================
'  Hilfsroutinen
' ===================================================================================

Private Function FldText(td As DAO.TableDef, name As String, size As Long) As DAO.Field
    Dim f As DAO.Field
    Set f = td.CreateField(name, dbText, size)
    Set FldText = f
End Function

Private Sub AddPrimaryKey(db As DAO.Database, tableName As String, fieldName As String)
    Dim td As DAO.TableDef
    Dim idx As DAO.Index
    Set td = db.TableDefs(tableName)

    Set idx = td.CreateIndex("PrimaryKey")
    idx.Primary = True
    idx.Unique = True
    idx.Fields.Append idx.CreateField(fieldName)
    td.Indexes.Append idx
End Sub

Private Sub CreateCascade(db As DAO.Database, relName As String, _
                          masterTable As String, childTable As String, _
                          masterField As String, childField As String)
    Dim rel As DAO.Relation
    Dim f As DAO.Field

    Set rel = db.CreateRelation(relName, masterTable, childTable, _
                                dbRelationDeleteCascade)
    Set f = rel.CreateField(masterField)
    f.ForeignName = childField
    rel.Fields.Append f
    db.Relations.Append rel
End Sub

Private Sub DropTableIfExists(db As DAO.Database, tableName As String)
    If TableExists(db, tableName) Then
        db.TableDefs.Delete tableName
        db.TableDefs.Refresh
    End If
End Sub

Private Function TableExists(db As DAO.Database, tableName As String) As Boolean
    Dim td As DAO.TableDef
    On Error Resume Next
    Set td = db.TableDefs(tableName)
    TableExists = (Err.Number = 0) And (Not td Is Nothing)
    Err.Clear
    On Error GoTo 0
End Function

Private Sub DropRelationIfExists(db As DAO.Database, relName As String)
    Dim i As Integer
    For i = db.Relations.Count - 1 To 0 Step -1
        If db.Relations(i).name = relName Then
            db.Relations.Delete relName
            Exit For
        End If
    Next i
End Sub
