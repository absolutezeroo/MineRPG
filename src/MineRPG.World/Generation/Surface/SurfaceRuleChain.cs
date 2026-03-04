using System;
using System.Collections.Generic;

namespace MineRPG.World.Generation.Surface;

/// <summary>
/// Evaluates an ordered list of surface rules. The first rule that returns
/// a block ID wins. Falls back to a default block if no rule matches.
/// Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class SurfaceRuleChain
{
    private readonly ISurfaceRule[] _rules;
    private readonly ushort _defaultStoneId;

    /// <summary>
    /// Creates a surface rule chain with the given rules and fallback block.
    /// </summary>
    /// <param name="rules">Ordered list of rules to evaluate.</param>
    /// <param name="defaultStoneId">Fallback block ID when no rule matches.</param>
    public SurfaceRuleChain(IReadOnlyList<ISurfaceRule> rules, ushort defaultStoneId)
    {
        if (rules == null)
        {
            throw new ArgumentNullException(nameof(rules));
        }

        _rules = new ISurfaceRule[rules.Count];

        for (int i = 0; i < rules.Count; i++)
        {
            _rules[i] = rules[i];
        }

        _defaultStoneId = defaultStoneId;
    }

    /// <summary>
    /// Evaluates all rules in order and returns the first matching block ID.
    /// </summary>
    /// <param name="context">The surface context to evaluate.</param>
    /// <returns>The block ID from the first matching rule, or the default stone.</returns>
    public ushort Evaluate(in SurfaceContext context)
    {
        for (int i = 0; i < _rules.Length; i++)
        {
            ushort? result = _rules[i].TryApply(in context);

            if (result.HasValue)
            {
                return result.Value;
            }
        }

        return _defaultStoneId;
    }
}
