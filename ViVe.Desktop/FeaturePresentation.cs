// Copyright (C) 2026 Richard M. Hamilton. First public release: 2026-11-09.
// SPDX-License-Identifier: GPL-3.0-only
namespace ViVe.Desktop;
public static class FeaturePresentation
{
    public static List<FeatureRow> VisibleRows(IEnumerable<FeatureRow> rows,bool details)
        => details ? rows.ToList() : rows.GroupBy(r=>r.Id)
            .Select(group=>group.FirstOrDefault(r=>r.Priority==r.EffectivePriority) ?? group.First())
            .OrderBy(r=>r.Id).ToList();
}
