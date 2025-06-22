# Migration Rules

Rules describe when entries should be copied between tiers in a tiered cache. A `MigrationRuleSet` aggregates multiple rules and can schedule periodic checks.

`MigrationRule<TKey,TValue>` defines source tier, destination tier and a predicate evaluating the entry.

### Example
```csharp
var rules = new MigrationRuleSet<string,int>();
// promote from tier 1 to tier 0 if value is frequently accessed
rules.AddRule(1, 0, (k,v,src,dst) => v > 10);
rules.SetPeriodicInterval(TimeSpan.FromMinutes(5));
```
