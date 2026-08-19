import xlwings as xw
import numpy as np
import datetime

@xw.func
def arrayparam(arr1,arr2):
    z=np.add(arr1, arr2)
    return z

@xw.func
def py_addvector(array1,array2):
    """add to vectors"""
    z=np.add(array1, array2)
    return z
 
@xw.func
def py_sumvector(array1):
    """add value to vector"""
    z=np.sum(array1)
    return z
 
@xw.func  
def py_sortvector(array1):
    """sort numbers in vector"""
    z=np.sort(array1)
    return z
    
@xw.func  
def py_normvector(array1,value):
    """sort numbers in vector"""
    z=np.divide(array1,value)
    z=np.multiply(z,100)
    return z
    
@xw.func  
@xw.arg('array1', numbers=int)
def py_divvector(array1,value):
    """sort numbers in vector"""
    z=np.divide(array1,value)
    return z    
    
@xw.func
@xw.arg('prozesswaerme', numbers=float)
@xw.arg('mo_anfang', numbers=int)
@xw.arg('mo_ende', numbers=int)
def py_monats_summe(prozesswaerme,mo_anfang,mo_ende):
    z=[0] * 12
    for indexMonat in range(0,12):
        result = prozesswaerme[mo_anfang[indexMonat]:mo_ende[indexMonat]+1]
        z[indexMonat]=sum(result)/1000
    return z

@xw.func  
def py_sumvectorrange(array1,index_anfang,index_ende):
    resultList = array1[index_anfang:index_ende+1]
    z=sum(resultList)/1000
    return z

@xw.func  
@xw.arg('tarifcode', numbers=int)
@xw.arg('Anfang_Sommer', numbers=int)
@xw.arg('Ende_Sommer', numbers=int)
@xw.arg('Anfang_Sommer_HT', numbers=int)
@xw.arg('Ende_Sommer_HT', numbers=int)
@xw.arg('Anfang_Winter_HT', numbers=int)
@xw.arg('Ende_Winter_HT', numbers=int)
def py_tarifcode(tarifcode, Anfang_Sommer, Ende_Sommer,
                    Anfang_Sommer_HT, Ende_Sommer_HT, 
                    Anfang_Winter_HT, Ende_Winter_HT):

    date = datetime.date.today()
    wochentag = datetime.date(date.year,1,1).weekday()
    wot = []
    wot.append(wochentag)
    
    for indexTage in range(1, 365):
        wochentag = (wochentag + 1) % 7
        wot.append(wochentag)
    
    indexTage = 0
    for indexStunden in range(0, 8759):
        if(indexStunden % 24 == 0 and indexStunden != 0):
                indexTage = indexTage + 1
      
        if(indexTage + 1 < Anfang_Sommer or indexTage + 1 > Ende_Sommer):
            if(indexStunden % 24 < Anfang_Winter_HT[wot[indexTage]] or indexStunden % 24 >= Ende_Winter_HT[wot[indexTage]]):
                tarifcode[indexStunden] = 4
            else:
                tarifcode[indexStunden] = 2
        else:
             if(indexStunden % 24 < Anfang_Sommer_HT[wot[indexTage]] or indexStunden % 24 >= Ende_Sommer_HT[wot[indexTage]]):
                 tarifcode[indexStunden] = 3
             else:
                 tarifcode[indexStunden] = 1
    return tarifcode    
    
@xw.func  
@xw.arg('array1', numbers=int)   
def py_maxvalue(array1):
	return np.max(array1)

@xw.func  
@xw.arg('tarifcode', numbers=int)   
@xw.arg('stromproduktion', numbers=float)   
@xw.arg('strombedarf', numbers=float)   
def py_strombezug(tarifcode,stromproduktion,strombedarf):
    strombezug = []
    #aus den seperaten Listarrays ein numpy Array generieren
    array_2d = list(zip(tarifcode,stromproduktion,strombedarf))
    nparray = np.array(array_2d)
    strombezug=np.zeros(8760)
    con = np.array(strombedarf) > np.array(stromproduktion)
    np.subtract(strombedarf, stromproduktion, where=con, out=strombezug)
    return strombezug	
    
@xw.func  
@xw.arg('tarifcode', numbers=int)   
@xw.arg('stromproduktion', numbers=float)   
@xw.arg('strombedarf', numbers=float)   
def py_stromeinspeisung(tarifcode,stromproduktion,strombedarf):
    stromeinspeisung = [0] * 8760
    #aus den seperaten Listarrays ein numpy Array generieren
    array_2d = list(zip(tarifcode,stromproduktion,strombedarf))
    nparray = np.array(array_2d)

    for indexStunden in range(0, 8759):
        if(strombedarf[indexStunden] > stromproduktion[indexStunden]):
            stromeinspeisung[indexStunden] = 0
        else:
            stromeinspeisung[indexStunden] = stromproduktion[indexStunden] - strombedarf[indexStunden]
    return stromeinspeisung	
    
@xw.func  
@xw.arg('tarifcode', numbers=int)   
@xw.arg('strombedarf', numbers=float)   
@xw.arg('strombezug', numbers=float)       
def py_maxmaxstrom_bedarf_bezug(tarifcode,strombedarf,strombezug):
    max_strombedarf = [0,0,0,0,0]
    max_strom_bezug = [0,0,0,0,0]

    #aus den seperaten Listarrays ein numpy Array generieren
    array_2d = list(zip(tarifcode,strombedarf,strombezug))
    nparray = np.array(array_2d)
    
    for i in range(1,5):
        fltr = np.asarray([i])
        a=nparray[np.in1d(nparray[:, 0], fltr)] 
        if a.size != 0:
            max_strombedarf[i]=np.max(a[:,1])
            max_strom_bezug[i]=np.max(a[:,2])

    max_strombedarf[0]=np.max(np.array(max_strombedarf))
    max_strom_bezug[0]=np.max(np.array(max_strom_bezug))
    return list(zip(max_strombedarf,max_strom_bezug))
    
@xw.func  
@xw.arg('tarifcode', numbers=int)   
@xw.arg('stromproduktion', numbers=float)   
@xw.arg('strombedarf', numbers=float)       
@xw.arg('strombezug', numbers=float)  
def py_summe_produktion_bedarf_bezug(tarifcode,stromproduktion,strombedarf,strombezug):
    summe_stromproduktion = [0,0,0,0,0]
    summe_strombedarf = [0,0,0,0,0]
    summe_strombezug = [0,0,0,0,0]
    eigenverbrauch_arbeit = [0,0,0,0,0]
    einsparung_arbeit = [0,0,0,0,0]

    #aus den seperaten Listarrays ein numpy Array generieren
    array_2d = list(zip(tarifcode,stromproduktion,strombedarf,strombezug))
    nparray = np.array(array_2d)
    for k in range(1, 5	):
    	fltr = np.asarray([k])
    	a=nparray[np.in1d(nparray[:, 0], fltr)] 
    	summe_stromproduktion[k] = py_divvector(py_sumvector(a[:,1]),1000)
    	summe_strombedarf[k] = py_divvector(py_sumvector(a[:,2]),1000)
    	summe_strombezug[k] = py_divvector(py_sumvector(a[:,3]),1000)
    	eigenverbrauch_arbeit[k] = summe_strombedarf[k] - summe_strombezug[k]
    	
    summe_stromproduktion[0] = py_sumvector(summe_stromproduktion)
    summe_strombedarf[0] = py_sumvector(summe_strombedarf)
    summe_strombezug[0] = py_sumvector(summe_strombezug)
    eigenverbrauch_arbeit[0] = py_sumvector(eigenverbrauch_arbeit)
    return list(zip(summe_stromproduktion,summe_strombedarf,summe_strombezug,eigenverbrauch_arbeit))
    
@xw.func  
@xw.arg('eigenverbrauch_arbeit', numbers=float)   
@xw.arg('Arbeitspreis', numbers=float)       
def py_einsparung_arbeit(eigenverbrauch_arbeit,Arbeitspreis):
    einsparung_arbeit = [0,0,0,0,0]

    for k in range(1, 5	):
    	einsparung_arbeit[k] = eigenverbrauch_arbeit[k] * 1000 * Arbeitspreis[k]
    einsparung_arbeit[0] = py_sumvector(einsparung_arbeit)
    return einsparung_arbeit
    
@xw.func  
@xw.arg('wo_prozesswaerme', numbers=float)   
@xw.arg('monatswaerme', numbers=float)  
@xw.arg('monatanfang', numbers=int)
@xw.arg('monatende', numbers=int)
def py_strom_wochetojahr(wo_prozesswaerme,monatswaerme,monatanfang,monatende):
    wot = [0] * 365
    prozwaerme_Monat = [0] * 12
    std_prozesswaerme = [0] * 8760
    std_monat = [0] * 8760
    prozesswerte = [0] * 8760

    #Wochentag 1. Januar bestimmen  
    date = datetime.date.today()
    wochentag = datetime.date(date.year,1,1).weekday()
 
    #Array für Jahres Wochentage   
    wot[0] = wochentag
    for indexTage in range(1, 365):
        wochentag = (wochentag + 1) % 7
        wot[indexTage] = wochentag
    
    wochentag = wot[0]
    indexTage = 0
    monat = 0
    for indexStunden in range(0, 8760):
        if(indexStunden % 24 == 0 and indexStunden != 0):
                indexTage = indexTage + 1
                #Wochentag bestimmen
                wochentag = wot[indexTage]
                #Monat bestimmen
                if(indexStunden > monatende[monat]):
                        monat=monat+1
        #Prozesswaerme und Monat über Jahresstunden Index
        std_prozesswaerme[indexStunden] = wo_prozesswaerme[(wochentag*24)+indexStunden % 24]
        std_monat[indexStunden] = monat
    
    #Summe Prozesswaerme monatsweise
    for indexMonat in range(0, 12):
        a=np.array(std_prozesswaerme)
        prozwaerme_Monat[indexMonat] = a[monatanfang[indexMonat]:monatende[indexMonat]+1].sum()

    for indexStunden in range(0, 8760):
        prozesswerte[indexStunden] = (std_prozesswaerme[indexStunden] * monatswaerme[std_monat[indexStunden]]*1000) /  prozwaerme_Monat[std_monat[indexStunden]]
    return prozesswerte    
    
@xw.func      
def py_monats_grenzen():
    monatsanfang = [0] * 12
    monatsende = [0] * 12 
    mtage = [28,31,30,31,30,31,31,30,31,30,31]
    
    monatsende[0] = 743
    for i in range(1,12):
        monatsende[i] = monatsende[i-1] + mtage[i-1] * 24
    
    monatsanfang[0] = 0
    for i in range(1,12):
        monatsanfang[i] = monatsende[i-1] +1
    return list(zip(monatsanfang,monatsende))  
    
@xw.func  
@xw.arg('tarifcode', numbers=int)   
@xw.arg('stromeinspeisung', numbers=float)   
def py_strom_ht_nt(tarifcode,stromeinspeisung):
    ret = [0] * 5
    array_2d = list(zip(tarifcode,stromeinspeisung))
    nparray = np.array(array_2d)
    
    for k in range(1, 5	):
    	fltr = np.asarray([k])
    	a=nparray[np.in1d(nparray[:, 0], fltr)] 
    	ret[k] = py_divvector(py_sumvector(a[:,1]),1000)
    return ret
    
@xw.func  
@xw.arg('TagTyp', numbers=int)   
@xw.arg('TagesVerteilung', numbers=float) 
@xw.arg('Heizlast', numbers=float) 
def py_StdWerte(TagTyp,TagesVerteilung,Heizlast):
    w_bedarf = [0] * 8760
    a = [[0] * 24 for i in range(8)]

    for k in range(0, 192):
        a[int(k/24)][k % 24] = TagesVerteilung[k]

    for k in range(0,365):
        for i in range(0,24):
            w_bedarf[k*24+i] = a[TagTyp[k]-1][i] * Heizlast[k]
    return w_bedarf    
    
    