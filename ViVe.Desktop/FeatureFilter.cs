// Copyright (C) 2026 Richard M. Hamilton. First public release: 2026-11-09.
// SPDX-License-Identifier: GPL-3.0-only
using System.Globalization;
namespace ViVe.Desktop;

public record FeatureRow(uint Id, string Name, string State, string Priority, uint Variant, uint Payload, string EffectiveState="Unknown",string EffectivePriority="Unavailable",string EffectiveDetail="Not queried");

public static class FeatureFilter
{
    public static bool Matches(FeatureRow row,string search,string state,string priority,string variant,bool effectiveState=false)
    {
        var query=search.Trim();
        return (query.Length==0 || row.Id.ToString(CultureInfo.InvariantCulture).Contains(query,StringComparison.Ordinal) || row.Name.Contains(query,StringComparison.OrdinalIgnoreCase))
            && (state=="All states" || (effectiveState ? row.EffectiveState : row.State)==state)
            && (priority=="All priorities" || row.Priority==priority)
            && (string.IsNullOrWhiteSpace(variant) || uint.TryParse(variant,NumberStyles.None,CultureInfo.InvariantCulture,out var value) && row.Variant==value);
    }
}
