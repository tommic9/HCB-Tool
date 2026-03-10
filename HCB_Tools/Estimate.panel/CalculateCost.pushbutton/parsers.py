# -*- coding: utf-8 -*-
import re

def to_mm(token):
    m = re.match(r'^([0-9]*\.?[0-9]+)(mm|cm|m)?$', token)
    if not m:
        m2 = re.match(r'^([0-9]*,[0-9]+)(mm|cm|m)?$', token)
        if not m2:
            return None
        val = float(m2.group(1).replace(',','.'))
        unit = m2.group(2) or 'mm'
    else:
        val = float(m.group(1))
        unit = m.group(2) or 'mm'
    if unit=='mm': return val
    if unit=='cm': return val*10.0
    if unit=='m':  return val*1000.0
    return None

def get_round_diams(size_str):
    if not size_str:
        return []
    s = size_str.strip().lower().replace(u'⌀','').replace('ø','').replace(' ','')
    tokens = re.split(r'[-/_x×]', s)
    vals = []
    for t in tokens:
        mm = to_mm(t)
        if mm is not None:
            vals.append(int(round(mm)))
        else:
            try:
                vals.append(int(round(float(t.replace(',','.')))))
            except Exception:
                pass
    return vals

def get_max_rect_dim(size_str):
    if not size_str:
        return None
    s = size_str.lower().replace(' ', '').replace(u'×','x')
    parts = s.split('-')
    maxdim = None
    for part in parts:
        m = re.match(r'^(\d+)x(\d+)$', part)
        if not m:
            continue
        w = int(m.group(1)); h = int(m.group(2))
        cand = max(w, h)
        maxdim = cand if (maxdim is None or cand > maxdim) else maxdim
    if maxdim is None:
        nums = re.findall(r"\d+", s)
        if nums:
            maxdim = max(int(n) for n in nums)
    return maxdim
