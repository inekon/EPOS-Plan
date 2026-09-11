"""Rainflow post-processing; cycle-life curve must come from a stated source.

Counting implementation: https://github.com/iamlikeme/rainflow (MIT, v3.2.0).
Miner damage is a model assumption, not a direct SoH measurement.
Temperature/calendar life must be modeled separately with calibrated parameters.
"""
from math import log,exp,isfinite


def cycle_damage(soc_history,life_curve):
    """life_curve = [(DoD_fraction, cycles_to_same_EOL), ...], strictly ordered.

    Log-linear interpolation within the supplied domain; no extrapolation.
    Returns half/full cycles and summed dimensionless Miner damage.
    Keep endpoint residue across chunks, or process the continuous full trace.
    """
    import rainflow
    if any(not isfinite(s) or not 0<=s<=1 for s in soc_history):
        raise ValueError('SoC must be finite and within [0,1]')
    if len(life_curve)<2 or any(not 0<d<=1 or n<=0 for d,n in life_curve):
        raise ValueError('At least two valid manufacturer curve points required')
    if any(life_curve[i][0]>=life_curve[i+1][0] for i in range(len(life_curve)-1)):
        raise ValueError('Cycle-life curve must be strictly sorted')
    result=[]
    for depth,mean,count,start,end in rainflow.extract_cycles(soc_history):
        if depth==0:continue
        if not life_curve[0][0]<=depth<=life_curve[-1][0]:
            raise ValueError('DoD outside calibrated life-curve domain')
        for (a,na),(b,nb) in zip(life_curve,life_curve[1:]):
            if a<=depth<=b:
                n=exp(log(na)+(depth-a)/(b-a)*(log(nb)-log(na)))
                break
        result.append({'dod':depth,'mean_soc':mean,'count':count,'start_index':start,
                       'end_index':end,'cycles_to_eol':n,'damage':count/n})
    return {'cycles':result,'miner_damage':sum(x['damage'] for x in result)}
