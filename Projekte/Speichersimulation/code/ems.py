"""AC-bus reference model. kW, kWh, EUR; battery power > 0 discharges.

See Spezifikation.md for the model contract and deliberate simplifications.
The physical engine and billing functions need only Python's standard library.
The horizon optimizer imports PuLP only when used.
"""
from dataclasses import dataclass
from datetime import datetime, timezone
from math import isfinite
from typing import Sequence

EPS = 1e-7
DT = 0.25


@dataclass(frozen=True)
class Battery:
    id: str
    capacity_kwh: float
    charge_kw: float
    discharge_kw: float
    eta_charge: float = 0.95
    eta_discharge: float = 0.95
    soc_min: float = 0.1
    soc_max: float = 0.9
    initial_soc: float = 0.5
    reserve_kwh: float = 0.0
    aux_kw: float = 0.0
    wear_eur_per_kwh_out: float = 0.03

    def __post_init__(self):
        values = [v for k, v in vars(self).items() if k != 'id']
        if not self.id or not all(isfinite(x) for x in values):
            raise ValueError('Invalid battery identifier or non-finite value')
        if not (self.capacity_kwh > 0 and self.charge_kw >= 0 and self.discharge_kw >= 0):
            raise ValueError('Invalid capacity or power')
        if not (0 < self.eta_charge <= 1 and 0 < self.eta_discharge <= 1):
            raise ValueError('Efficiency must be in (0, 1]')
        if not (0 <= self.soc_min <= self.initial_soc <= self.soc_max <= 1):
            raise ValueError('Invalid SoC limits or initial SoC')
        if not (0 <= self.reserve_kwh <= self.emax - self.emin):
            raise ValueError('Reserve exceeds usable capacity')
        if self.aux_kw < 0 or self.wear_eur_per_kwh_out < 0:
            raise ValueError('Negative auxiliary load or wear cost')

    @property
    def emin(self):
        return self.soc_min * self.capacity_kwh

    @property
    def emax(self):
        return self.soc_max * self.capacity_kwh


def timestamp(value):
    d = datetime.fromisoformat(value.replace('Z', '+00:00'))
    if d.tzinfo is None:
        raise ValueError('Timezone offset required')
    return d.astimezone(timezone.utc)


def validate_rows(rows):
    if not rows:
        raise ValueError('Empty input')
    previous = None
    for row in rows:
        t = timestamp(row['timestamp'])
        if int(t.timestamp()) % 900 or t.microsecond:
            raise ValueError('Timestamp not on the quarter-hour grid')
        if previous is not None and (t - previous).total_seconds() != 900:
            raise ValueError('Gap, duplicate, or unsorted timestamps')
        for key in ('load_kw', 'pv_kw', 'buy_eur_kwh', 'sell_eur_kwh'):
            if not isfinite(float(row[key])):
                raise ValueError(f'Invalid {key}')
        if min(row['load_kw'], row['pv_kw']) < 0:
            raise ValueError('Load and available AC PV must be nonnegative')
        previous = t


def limits(b, e, release_reserve=False, dt=DT):
    if not b.emin - EPS <= e <= b.emax + EPS:
        raise ValueError('State outside physical energy bounds')
    floor = b.emin if release_reserve else b.emin + b.reserve_kwh
    discharge = min(b.discharge_kw, max(0.0, e - floor) * b.eta_discharge / dt)
    charge = min(b.charge_kw, max(0.0, b.emax - e) / (b.eta_charge * dt))
    return charge, discharge


def allocate(request, batteries, energies, allocation='balanced', release=False, rotation=0):
    """Bounded water filling or rotating cascade; common direction for the fleet."""
    if allocation not in ('balanced', 'cascade', 'merit'):
        raise ValueError('Unknown allocation policy')
    discharge = request >= 0
    caps = [limits(b, e, release)[int(discharge)] for b, e in zip(batteries, energies)]
    out = [0.0] * len(batteries)
    remaining = min(abs(request), sum(caps))
    if allocation != 'balanced':
        order = list(range(len(batteries)))
        if allocation == 'cascade' and order:
            shift = rotation % len(order)
            order = order[shift:] + order[:shift]
        elif allocation == 'merit':
            order.sort(key=lambda j: (
                batteries[j].wear_eur_per_kwh_out if discharge else 0,
                -(batteries[j].eta_discharge if discharge else batteries[j].eta_charge),
                batteries[j].id))
        for j in order:
            out[j] = min(caps[j], remaining)
            remaining -= out[j]
    else:
        weights = [max(EPS, (e - b.emin - (0 if release else b.reserve_kwh))
                       if discharge else b.emax - e) for b, e in zip(batteries, energies)]
        active = {j for j, cap in enumerate(caps) if cap > EPS}
        while remaining > EPS and active:
            total = sum(weights[j] for j in active)
            proposed = {j: remaining * weights[j] / total for j in active}
            saturated = {j for j in active if proposed[j] >= caps[j] - out[j]}
            if not saturated:
                for j in active:
                    out[j] += proposed[j]
                break
            for j in saturated:
                extra = caps[j] - out[j]
                out[j] += extra
                remaining -= extra
            active -= saturated
    return [p if discharge else -p for p in out]


def execute(row, batteries, energies, commands, *, grid_charge=True,
            battery_export=False, import_limit_kw=1e6, export_limit_kw=1e6,
            peak_target_kw=None, allocation='balanced', rotation=0, curtailment_request_kw=0.0):
    if len(commands) != len(batteries) or len(energies) != len(batteries):
        raise ValueError('One command and energy state per battery required')
    if not all(isfinite(x) for x in commands):
        raise ValueError('Non-finite command')
    if min(commands, default=0) < -EPS and max(commands, default=0) > EPS:
        raise ValueError('Opposing battery directions are disabled')
    if min(import_limit_kw, export_limit_kw) < 0:
        raise ValueError('Invalid connection limit')
    aux = sum(b.aux_kw for b in batteries)
    requested_curtail = min(max(0.0, curtailment_request_kw), max(0.0,row['pv_kw']-row['load_kw']-aux))
    n = row['load_kw'] - row['pv_kw'] + aux + requested_curtail
    release = peak_target_kw is not None and n > peak_target_kw
    if release and sum(commands) < n - peak_target_kw - 1e-5:
        commands = allocate(max(sum(commands), n - peak_target_kw), batteries,
                            energies, allocation, True, rotation)
    clipped = []
    for b, e, p in zip(batteries, energies, commands):
        ch, dis = limits(b, e, release)
        clipped.append(max(-ch, min(dis, p)))
    total = sum(clipped)
    charge_ceiling = max(0.0, import_limit_kw - n)
    if peak_target_kw is not None:
        charge_ceiling = min(charge_ceiling, max(0.0, peak_target_kw - n))
    if not grid_charge:
        charge_ceiling = min(charge_ceiling, max(0.0, -n))
    allowed = max(total, -charge_ceiling)
    if not battery_export:
        allowed = min(allowed, max(0.0, n))
    # Preserve PV exports; prohibit incremental battery exports beyond the limit.
    allowed = min(allowed, max(0.0, n + export_limit_kw))
    if abs(total) > EPS and abs(allowed) < abs(total):
        clipped = [p * allowed / total for p in clipped]
    losses, next_energies = [], []
    for b, e, p in zip(batteries, energies, clipped):
        c, d = max(0, -p), max(0, p)
        enew = e + (b.eta_charge * c - d / b.eta_discharge) * DT
        loss = ((1 - b.eta_charge) * c + (1 / b.eta_discharge - 1) * d) * DT
        if not b.emin - EPS <= enew <= b.emax + EPS:
            raise ArithmeticError('Energy balance violated')
        next_energies.append(enew)
        losses.append(loss)
    raw_grid = n - sum(clipped)
    extra_curtail = min(row['pv_kw']-requested_curtail, max(0.0, -export_limit_kw - raw_grid))
    curtailed = requested_curtail + extra_curtail
    grid = raw_grid + extra_curtail
    return {**row, 'grid_kw': grid, 'import_kw': max(0, grid), 'export_kw': max(0, -grid),
            'battery_kw': clipped, 'energy_start_kwh': list(energies),
            'energy_end_kwh': next_energies, 'loss_kwh': sum(losses), 'aux_kw': aux,
            'curtailed_kw': curtailed, 'import_violation_kw': max(0, grid - import_limit_kw),
            'peak_violation_kw': max(0, grid - peak_target_kw) if peak_target_kw is not None else 0,
            'command_deviation_kw': sum(abs(a-b) for a,b in zip(commands, clipped))}


def simulate(rows, batteries, strategy='pv_greedy', *, allocation='balanced',
             peak_target_kw=None, buy_threshold=0.1, sell_threshold=0.3,
             planned=None, planned_curtailment=None, initial_energies=None, **options):
    validate_rows(rows)
    if len({b.id for b in batteries}) != len(batteries):
        raise ValueError('Duplicate battery identifiers')
    if strategy not in ('pv_greedy', 'peak', 'threshold', 'planned'):
        raise ValueError('Unknown strategy')
    if strategy == 'peak' and peak_target_kw is None:
        raise ValueError('Peak target required')
    if strategy == 'planned' and (planned is None or len(planned) != len(rows)):
        raise ValueError('One planned command vector per interval required')
    if planned_curtailment is not None and len(planned_curtailment) != len(rows):
        raise ValueError('One curtailment request per interval required')
    energies = list(initial_energies) if initial_energies is not None else [b.initial_soc*b.capacity_kwh for b in batteries]
    result = []
    for i, row in enumerate(rows):
        n = row['load_kw'] - row['pv_kw'] + sum(b.aux_kw for b in batteries)
        rotation = int(timestamp(row['timestamp']).timestamp()) // 86400
        if strategy == 'planned':
            commands = planned[i]
        else:
            if strategy == 'peak':
                request = n - peak_target_kw
            elif strategy == 'pv_greedy':
                request = n
            elif row['buy_eur_kwh'] <= buy_threshold:
                request = -sum(b.charge_kw for b in batteries)
            elif row['buy_eur_kwh'] >= sell_threshold:
                request = max(0, n)  # Threshold benchmark: avoided retail purchase.
            else:
                request = 0
            commands = allocate(request, batteries, energies, allocation,
                                peak_target_kw is not None and n > peak_target_kw, rotation)
        rec = execute(row, batteries, energies, commands, allocation=allocation,
                      rotation=rotation, peak_target_kw=peak_target_kw,
                      curtailment_request_kw=planned_curtailment[i] if planned_curtailment is not None else 0.0, **options)
        result.append(rec)
        energies = rec['energy_end_kwh']
    return result


def bill(records, demand_eur_kw=0.0, fixed_eur=0.0):
    """Exactly ONE billing period. Never annualize a short input silently."""
    if demand_eur_kw < 0:
        raise ValueError('Negative demand tariff')
    energy = sum((r['import_kw']*r['buy_eur_kwh'] - r['export_kw']*r['sell_eur_kwh'])*DT for r in records)
    peak = max((r['import_kw'] for r in records), default=0.0)
    return {'energy_eur': energy, 'demand_eur': peak*demand_eur_kw,
            'peak_kw': peak, 'total_eur': energy+peak*demand_eur_kw+fixed_eur}


def npv(capex, annual_cashflows, discount_rate, residual_value=0.0):
    if capex < 0 or discount_rate <= -1 or not annual_cashflows:
        raise ValueError('Invalid project cashflows')
    return -capex + sum(cf/(1+discount_rate)**year for year,cf in enumerate(annual_cashflows, 1)) + residual_value/(1+discount_rate)**len(annual_cashflows)


def optimize_horizon(rows, batteries, energies, *, mode='economic', grid_charge=True,
                     battery_export=False, import_limit_kw=1e6, export_limit_kw=1e6,
                     peak_target_kw=None, historical_peak_kw=0.0, demand_eur_kw=0.0,
                     terminal_energies=None, time_limit_s=60):
    """MILP, constant efficiency and auxiliary load; returns AC command vectors.

    Caller supplies only forecasts already available at its decision time.
    Explicit terminal targets are required; default is cyclic within this horizon.
    Infeasible/timeout/no-optimal solution raises; caller must log and fall back.
    """
    import pulp as lp
    validate_rows(rows)
    if mode not in ('economic', 'pv') or not batteries:
        raise ValueError('Invalid optimizer mode or empty battery fleet')
    if len(energies) != len(batteries):
        raise ValueError('Energy vector length mismatch')
    if mode == 'pv':
        grid_charge, battery_export = False, False
    terminal = list(energies if terminal_energies is None else terminal_energies)
    if len(terminal) != len(batteries):
        raise ValueError('Terminal vector length mismatch')
    for b, e, end in zip(batteries, energies, terminal):
        if not b.emin <= e <= b.emax or not b.emin + b.reserve_kwh <= end <= b.emax:
            raise ValueError('Initial or terminal energy outside limits')
    T, B = range(len(rows)), range(len(batteries))
    prob = lp.LpProblem('AC_multi_battery', lp.LpMinimize)
    c = {(j,t):lp.LpVariable(f'c_{j}_{t}',0,batteries[j].charge_kw) for j in B for t in T}
    d = {(j,t):lp.LpVariable(f'd_{j}_{t}',0,batteries[j].discharge_kw) for j in B for t in T}
    e = {(j,t):lp.LpVariable(f'e_{j}_{t}',batteries[j].emin,batteries[j].emax) for j in B for t in range(len(rows)+1)}
    direction = {t:lp.LpVariable(f'direction_{t}',cat='Binary') for t in T}
    grid_dir = {t:lp.LpVariable(f'grid_dir_{t}',cat='Binary') for t in T}
    imp = {t:lp.LpVariable(f'imp_{t}',0,import_limit_kw) for t in T}
    exp = {t:lp.LpVariable(f'exp_{t}',0,export_limit_kw) for t in T}
    cur = {t:lp.LpVariable(f'cur_{t}',0,max(0,rows[t]['pv_kw']-rows[t]['load_kw']-sum(b.aux_kw for b in batteries))) for t in T}
    # Finite, data-derived big-M values tighten the binary constraints.
    aux = sum(b.aux_kw for b in batteries)
    peak = lp.LpVariable('peak',lowBound=historical_peak_kw)
    exceed = lp.LpVariable('peak_exceed',lowBound=0)
    for j,b in enumerate(batteries):
        prob += e[j,0] == energies[j]
        prob += e[j,len(rows)] == terminal[j]
        for t in T:
            prob += c[j,t] <= b.charge_kw*direction[t]
            prob += d[j,t] <= b.discharge_kw*(1-direction[t])
            prob += e[j,t+1] == e[j,t]+DT*(b.eta_charge*c[j,t]-d[j,t]/b.eta_discharge)
            prob += e[j,t+1] >= b.emin+b.reserve_kwh
    for t,r in enumerate(rows):
        n = r['load_kw']-r['pv_kw']+aux
        cs, ds = lp.lpSum(c[j,t] for j in B), lp.lpSum(d[j,t] for j in B)
        prob += imp[t]-exp[t] == n+cs-ds+cur[t]
        prob += imp[t] <= min(import_limit_kw,r['load_kw']+aux+sum(b.charge_kw for b in batteries))*grid_dir[t]
        prob += exp[t] <= min(export_limit_kw,r['pv_kw']+sum(b.discharge_kw for b in batteries))*(1-grid_dir[t])
        if not grid_charge:
            prob += cs+cur[t] <= max(0,-n)
        if not battery_export:
            prob += ds <= max(0,n)
        prob += peak >= imp[t]
        if peak_target_kw is not None:
            prob += imp[t] <= peak_target_kw+exceed
    solver = lp.PULP_CBC_CMD(msg=False, timeLimit=time_limit_s, threads=1, gapRel=0)
    def solve(objective):
        prob.setObjective(objective)
        prob.solve(solver)
        if prob.status != lp.LpStatusOptimal or prob.sol_status != lp.LpSolutionOptimal:
            raise RuntimeError(f'Optimizer did not prove optimality: {lp.LpStatus[prob.status]}')
    if peak_target_kw is not None:
        solve(exceed)
        prob += exceed <= exceed.value()+1e-6
    if mode == 'pv':
        imports = DT*lp.lpSum(imp[t] for t in T)
        solve(imports)
        prob += imports <= lp.value(imports)+1e-6
        curtail = DT*lp.lpSum(cur[t] for t in T)
        solve(curtail)
        prob += curtail <= lp.value(curtail)+1e-6
        objective = DT*lp.lpSum(e[j,t+1] for j in B for t in T)
    else:
        objective = DT*lp.lpSum(rows[t]['buy_eur_kwh']*imp[t]-rows[t]['sell_eur_kwh']*exp[t]
                     +lp.lpSum(batteries[j].wear_eur_per_kwh_out*d[j,t] for j in B) for t in T)
        objective += demand_eur_kw*(peak-historical_peak_kw)
    solve(objective)
    commands = [[float(d[j,t].value()-c[j,t].value()) for j in B] for t in T]
    return {'commands':commands,'objective':lp.value(objective),'status':'optimal',
            'terminal_energies':[e[j,len(rows)].value() for j in B],
            'planned_peak_kw':max(imp[t].value() for t in T),
            'planned_curtailment_kw':[cur[t].value() for t in T]}
