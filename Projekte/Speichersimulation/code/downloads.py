"""Public data adapters with immutable snapshots and strict UTC normalization.

Sources, API docs and licenses:
  Energy-Charts / Fraunhofer ISE: https://api.energy-charts.info/openapi.json
  DE-LU source data: Bundesnetzagentur | SMARD.de, CC BY 4.0 (API license_info)
  Open-Meteo: https://open-meteo.com/en/docs/historical-forecast-api
  Forecast.Solar: https://forecast.solar/ and https://doc.forecast.solar/api:estimate

An actual site's load is a local meter export, never a national load download.
Archived weather is a modeled PV reference, not a vintage day-ahead forecast.
"""
import argparse
import csv
from datetime import datetime, timedelta, timezone
from email.utils import parsedate_to_datetime
import hashlib
import json
import math
from pathlib import Path
import time
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode
from urllib.request import Request, urlopen
from zoneinfo import ZoneInfo

UTC = timezone.utc


def parse_time(s):
    value = datetime.fromisoformat(s.replace('Z','+00:00'))
    if value.tzinfo is None:
        raise ValueError('Use an ISO timestamp with UTC offset, e.g. 2025-01-01T00:00:00+01:00')
    return value.astimezone(UTC)


def iso(d):
    return d.astimezone(UTC).isoformat().replace('+00:00','Z')


def fetch_json(url, cache_dir, refresh=False, retries=3):
    """Persist source URL, retrieval time, raw payload, and SHA256.

    No silent replacement with zeros. A cached snapshot is reused unless
    --refresh is supplied. Retrying >60 s is delegated to a later CLI run.
    """
    cache = Path(cache_dir)
    cache.mkdir(parents=True,exist_ok=True)
    key = hashlib.sha256(url.encode()).hexdigest()
    pointer = cache/(key+'.latest.json')
    if pointer.exists() and not refresh:
        snap = json.loads((cache/json.loads(pointer.read_text())['snapshot']).read_text(encoding='utf-8'))
        raw = snap['raw_json']
        if hashlib.sha256(raw.encode()).hexdigest() != snap['sha256']:
            raise ValueError('Cached data checksum mismatch')
        return json.loads(raw), {k:v for k,v in snap.items() if k!='raw_json'}
    for attempt in range(retries+1):
        try:
            req = Request(url,headers={'User-Agent':'Speichersimulation/1.0','Accept':'application/json'})
            with urlopen(req,timeout=30) as response:
                raw = response.read().decode('utf-8')
            data = json.loads(raw)
            if data.get('error'):
                raise ValueError(f"API error: {data.get('reason', data['error'])}")
            now = datetime.now(UTC)
            snap = {'source_url':url,'retrieved_at':iso(now),'sha256':hashlib.sha256(raw.encode()).hexdigest(),'raw_json':raw}
            name = key+'_'+now.strftime('%Y%m%dT%H%M%S%fZ')+'.json'
            (cache/name).write_text(json.dumps(snap,ensure_ascii=False,indent=2),encoding='utf-8')
            pointer.write_text(json.dumps({'snapshot':name}),encoding='utf-8')
            return data,{k:v for k,v in snap.items() if k!='raw_json'}
        except HTTPError as err:
            if err.code not in (429,500,502,503,504) or attempt==retries:
                raise
            wait = min(2**attempt,30)
            retry_after = err.headers.get('Retry-After')
            if retry_after:
                try:
                    wait = max(wait,float(retry_after))
                except ValueError:
                    wait = max(wait,(parsedate_to_datetime(retry_after)-datetime.now(UTC)).total_seconds())
            if wait>60:
                raise RuntimeError(f'Rate limited; retry after {wait:.0f} seconds') from err
            time.sleep(wait)
        except (URLError,TimeoutError):
            if attempt==retries:
                raise
            time.sleep(2**attempt)
    raise RuntimeError('Download exhausted')


def price_intervals(data, start, end):
    if data.get('deprecated'):
        raise ValueError('Source endpoint marked deprecated; review adapter')
    if data.get('unit') != 'EUR / MWh' and data.get('unit') != 'EUR/MWh':
        raise ValueError(f"Unexpected price unit: {data.get('unit')}")
    ts, prices = data['unix_seconds'], data['price']
    if len(ts)!=len(prices) or len(ts)<2:
        raise ValueError('Invalid price arrays')
    out = []
    for i in range(len(ts)-1):
        a,b = datetime.fromtimestamp(ts[i],UTC),datetime.fromtimestamp(ts[i+1],UTC)
        if b<=a:
            raise ValueError('Duplicate or unsorted price timestamps')
        if b-a not in (timedelta(minutes=15),timedelta(minutes=60)):
            raise ValueError('Price gap or unsupported source interval')
        if b<=start or a>=end:
            continue
        # DE-LU quarter-hour market from 2025-10-01; reject an unexplained hole.
        if a>=datetime(2025,9,30,22,tzinfo=UTC) and b-a!=timedelta(minutes=15):
            raise ValueError('Expected a quarter-hour DE-LU price after 2025-10-01')
        p = prices[i]
        if p is None or not math.isfinite(p):
            raise ValueError('Missing price is not zero')
        out.append((a,b,p/1000.0))
    return out


def resample(intervals, start, end):
    """Overlap-weighted integration of interval means, with complete coverage."""
    if start>=end or int(start.timestamp())%900 or int(end.timestamp())%900:
        raise ValueError('Invalid target period')
    for j,(a,b,v) in enumerate(intervals):
        if a>=b or not math.isfinite(v) or (j and intervals[j-1][1]!=a):
            raise ValueError('Invalid, overlapping, or gapped source intervals')
    out=[]
    j=0
    t=start
    while t<end:
        u=t+timedelta(minutes=15)
        while j<len(intervals) and intervals[j][1]<=t:
            j+=1
        k=j
        covered=weighted=0.0
        while k<len(intervals) and intervals[k][0]<u:
            a,b,v=intervals[k]
            seconds=max(0,(min(b,u)-max(a,t)).total_seconds())
            covered+=seconds
            weighted+=seconds*v
            k+=1
        if abs(covered-900)>1e-6:
            raise ValueError(f'Incomplete data at {iso(t)}: {covered} seconds')
        out.append({'timestamp':iso(t),'value':weighted/900})
        t=u
    return out


def download_prices(start,end,cache_dir,refresh=False):
    """DE-LU historical prices. Explicit UTC endpoints and a guard interval."""
    query=urlencode({'bzn':'DE-LU','start':iso(start),'end':iso(end+timedelta(hours=1))})
    data,meta=fetch_json('https://api.energy-charts.info/price?'+query,cache_dir,refresh)
    intervals=price_intervals(data,start,end)
    meta.update({'source':'Energy-Charts.info / Fraunhofer ISE; Bundesnetzagentur | SMARD.de',
                 'license_info':data.get('license_info'),'input_unit':data['unit'],
                 'output_unit':'EUR/kWh','kind':'historical_market_price',
                 'known_at':None,'source_resolutions_minutes':sorted({(b-a).total_seconds()/60 for a,b,_ in intervals})})
    return resample(intervals,start,end),meta


def download_pv_history(start,end,lat,lon,tilt,azimuth,kwp,inverter_kw,pr,cache_dir,refresh=False):
    """Hourly archived weather -> approximate AC PV, then energy-preserving 15 min.

    Model: P_AC = min(P_inverter, P_STC * GTI / 1000 * performance_ratio).
    This is a modeled reference trajectory, NOT measured PV or a past forecast.
    Open-Meteo GTI timestamp marks the END of the preceding hour.
    """
    if not (kwp>0 and inverter_kw>0 and 0<pr<=1 and 0<=tilt<=90 and -180<=azimuth<=180):
        raise ValueError('Invalid PV system parameters')
    query=urlencode({'latitude':lat,'longitude':lon,'start_date':(start-timedelta(days=1)).date().isoformat(),
        'end_date':end.date().isoformat(),'hourly':'global_tilted_irradiance',
        'tilt':tilt,'azimuth':azimuth,'timezone':'UTC'})
    data,meta=fetch_json('https://historical-forecast-api.open-meteo.com/v1/forecast?'+query,cache_dir,refresh)
    if data['hourly_units']['global_tilted_irradiance']!='W/m²':
        raise ValueError('Unexpected irradiation unit')
    hours=data['hourly']['time']; values=data['hourly']['global_tilted_irradiance']
    if len(hours)!=len(values):
        raise ValueError('Weather array length mismatch')
    intervals=[]
    for s,value in zip(hours,values):
        b=datetime.fromisoformat(s).replace(tzinfo=UTC)
        a=b-timedelta(hours=1)
        if a>=end or b<=start:
            continue
        if value is None or not math.isfinite(value) or value<0:
            raise ValueError('Missing/invalid irradiation')
        intervals.append((a,b,min(inverter_kw,kwp*value/1000*pr)))
    meta.update({'kind':'modeled_pv_reference_not_vintage_forecast','known_at':None,
        'input_unit':'W/m2','output_unit':'kW AC','native_interval_minutes':60,
        'model':'min(inverter_kw, kwp * GTI / 1000 * performance_ratio)',
        'pv_parameters':{'lat':lat,'lon':lon,'tilt':tilt,'azimuth':azimuth,'kwp':kwp,'inverter_kw':inverter_kw,'performance_ratio':pr},
        'source':'Open-Meteo; underlying weather model attribution in cached response'})
    return resample(intervals,start,end),meta


def download_pv_forecast(lat,lon,tilt,azimuth,kwp,cache_dir,refresh=False):
    """Latest Forecast.Solar forecast as original watt samples, with UTC times.

    Samples are NOT silently treated as quarter-hour interval mean powers.
    Integrate the provider's energy increments using its timestamp convention
    before joining this output into the simulation; keep this issuance snapshot.
    """
    if not (-90<=lat<=90 and -180<=lon<=180 and 0<=tilt<=90 and -180<=azimuth<=180 and kwp>0):
        raise ValueError('Invalid PV site')
    url=f'https://api.forecast.solar/estimate/{lat}/{lon}/{tilt}/{azimuth}/{kwp}'
    data,meta=fetch_json(url,cache_dir,refresh)
    if data.get('message',{}).get('type')=='error':
        raise ValueError('Forecast.Solar returned an error')
    zone=ZoneInfo(data['message']['info']['timezone'])
    points=[]
    for s,w in data['result']['watts'].items():
        local=datetime.fromisoformat(s)
        if local.tzinfo is None:
            first=local.replace(tzinfo=zone,fold=0)
            second=local.replace(tzinfo=zone,fold=1)
            if first.utcoffset()!=second.utcoffset():
                raise ValueError('Ambiguous forecast timestamp; provider offset required')
            local=first
        if w is None or not math.isfinite(w) or w<0:
            raise ValueError('Invalid PV forecast sample')
        points.append({'timestamp':iso(local),'pv_sample_kw':w/1000})
    points.sort(key=lambda r:r['timestamp'])
    meta.update({'kind':'current_pv_forecast_samples','known_at':meta['retrieved_at'],
                 'sample_semantics':'provider watts samples; not interval means',
                 'source':'Forecast.Solar','native_payload_retained':True})
    return points,meta


def write_output(path,rows,meta):
    path=Path(path);path.parent.mkdir(parents=True,exist_ok=True)
    with path.open('w',newline='',encoding='utf-8') as f:
        writer=csv.DictWriter(f,fieldnames=list(rows[0]));writer.writeheader();writer.writerows(rows)
    meta['output_sha256']=hashlib.sha256(path.read_bytes()).hexdigest()
    path.with_suffix(path.suffix+'.meta.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2),encoding='utf-8')


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('source',choices=['prices','pv-history','pv-forecast'])
    p.add_argument('--start');p.add_argument('--end')
    p.add_argument('--out',required=True);p.add_argument('--cache',default='data/cache')
    p.add_argument('--refresh',action='store_true')
    for field in ('lat','lon','tilt','azimuth','kwp','inverter-kw','pr'):
        p.add_argument('--'+field,type=float)
    a=p.parse_args()
    if a.source=='prices':
        rows,meta=download_prices(parse_time(a.start),parse_time(a.end),a.cache,a.refresh)
    elif a.source=='pv-history':
        if any(v is None for v in (a.lat,a.lon,a.tilt,a.azimuth,a.kwp,a.inverter_kw,a.pr)):
            p.error('pv-history requires --lat --lon --tilt --azimuth --kwp --inverter-kw --pr')
        rows,meta=download_pv_history(parse_time(a.start),parse_time(a.end),a.lat,a.lon,a.tilt,a.azimuth,a.kwp,a.inverter_kw,a.pr,a.cache,a.refresh)
    else:
        if any(v is None for v in (a.lat,a.lon,a.tilt,a.azimuth,a.kwp)):
            p.error('pv-forecast requires --lat --lon --tilt --azimuth --kwp')
        rows,meta=download_pv_forecast(a.lat,a.lon,a.tilt,a.azimuth,a.kwp,a.cache,a.refresh)
    write_output(a.out,rows,meta)
    print(f'{len(rows)} rows written to {a.out}')
