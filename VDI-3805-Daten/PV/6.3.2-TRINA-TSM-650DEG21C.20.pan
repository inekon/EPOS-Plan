PVObject_=pvModule
  Version=6.85
  Flags=$00900443

  PVObject_Commercial=pvCommercial
    Comment=www.trinasolar.com  (China)
    Flags=$0041
    Manufacturer=Trina Solar
    Model=TSM-650DEG21C.20
    DataSource=TSL_2020_12
    YearBeg=2020
    Width=1.303
    Height=2.384
    Depth=0.040
    Weight=38.700
    NPieces=100
    PriceDate=24/06/20 16:13
    Remarks, Count=1
      Str_1
    End of Remarks
  End of PVObject pvCommercial

  Technol=mtSiMono
  NCelS=66
  NCelP=2
  NDiode=3
  SubModuleLayout=slTwinHalfCells
  GRef=1000
  TRef=25.0
  PNom=650.0
  PNomTolLow=0.00
  PNomTolUp=3.00
  BifacialityFactor=0.700
  Isc=18.350
  Voc=45.50
  Imp=17.270
  Vmp=37.70
  muISC=7.32
  muVocSpec=-114.0
  muPmpReq=-0.340
  RShunt=20000
  Rp_0=80000
  Rp_Exp=5.50
  RSerie=0.168
  Gamma=1.020
  muGamma=-0.0002
  VMaxIEC=1500
  VMaxUL=1500
  Absorb=0.90
  ARev=3.200
  BRev=10.360
  RDiode=0.010
  VRevDiode=-0.70
  AirMassRef=1.500
  CellArea=220.5
  SandiaAMCorr=50.000

  PVObject_IAM=pvIAM
    Flags=$00
    IAMMode=UserProfile
    IAMProfile=TCubicProfile
      NPtsMax=9
      NPtsEff=9
      LastCompile=$B18D
      Mode=3
      Point_1=0.0,1.00000
      Point_2=40.0,1.00000
      Point_3=50.0,0.99800
      Point_4=60.0,0.99200
      Point_5=70.0,0.98300
      Point_6=75.0,0.96100
      Point_7=80.0,0.93300
      Point_8=85.0,0.85300
      Point_9=90.0,0.00000
    End of TCubicProfile
  End of PVObject pvIAM
End of PVObject pvModule
