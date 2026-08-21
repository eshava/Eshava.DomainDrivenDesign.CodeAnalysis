# Eshava.DomainDrivenDesign.CodeAnalysis — Repository Notes

Roslyn source generator that produces the boilerplate the `Eshava.DomainDrivenDesign` approach
requires. That boilerplate runs through all four layers and, if the approach is followed
strictly, is hard to avoid by hand. Published as the NuGet package
**`Eshava.DomainDrivenDesign.CodeAnalysis`**.

**Conventions:** documentation, code and commit messages are written in English. Line endings are
pinned through `.gitattributes` — anything that may run on Linux must be checked out with LF.

## Layout

| Project | Content |
|---|---|
| `Eshava.DomainDrivenDesign.CodeAnalysis` | The generator library: templates, factories, analysis models. `netstandard2.0`, as Roslyn requires. |
| `SourceGenerator/` | The JSON configuration files driving the example generation. |
| `Eshava.Example.SourceGenerator` | The generator project of the sample — one generator class per layer plus an extension class initialising the context. |
| `Eshava.Example.SourceGenerator.Tests` | Tests for the generated output. |
| `Eshava.Example.Domain` · `.Application` · `.Infrastructure` · `.Api` | The sample solution the generators write into. |

## Rules

* **Never call `SyntaxFactory.ParseStatement` or any other string-parsing syntax API.** Syntax
  elements are assembled individually, piece by piece — that is what `Eshava.CodeAnalysis`
  provides, and parsing a string bypasses it.
* **Generated classes are always `partial`.** Generated files cannot be edited, so `partial` is
  what makes hand-written additions possible anywhere. Extension points are provided as virtual
  methods, and the `ApiGenerator` / `ApplicationGenerator` classes additionally accept code
  snippets.
* **Report progress visibly during long-running work,** so a running process is not mistaken for
  a stuck one.
* One generator class per layer, and currently **one project per layer** — the generator package
  does not support more.
* The generator project must target `netstandard2.0` and be referenced by every other project of
  the solution so the code is generated at compile time. The factory classes can also be run
  manually outside a generator, writing the returned text to disk.

## Where Condition Code Snippets

A code snippet marked `IsFilter` with `WhereClause.ForceAsWhereCondition` does not only add a
condition — it also decides which tables the generated query has to join, because the filtered
property usually sits on a model several references away from the one being queried. Two pieces
of code have to agree on that set, and they are far apart:

| Step | Where | Produces |
|---|---|---|
| `CollectApplicableWhereConditionCodeSnippets` | `InfrastructureTemplateMethods` | one `ApplicableWhereConditionCodeSnippet` per reachable model carrying the property, each with the model chain leading to it |
| `AddMissingQueryAnalysisItemsForApplicableCodeSnippets` | `InfrastructureTemplateMethods` | one `QueryAnalysisItem` — one SQL join — per edge of every chain |
| `AddCodeSnippetFilterConditions` | `QueryRepositoryTemplate` | the filter expression, **one per snippet and property** |

**The collection walk is breadth first on purpose. Do not turn it back into a recursion.**

It used to be a depth first walk that removed each model from the processed set on the way out,
so it enumerated *all simple paths* to the filtered property rather than the shortest one. Every
one of those paths became an applicable snippet, and every edge of every path became a join —
while the filter expression was reduced to a single condition further down. Joins and conditions
were computed from different populations, and the joins lost.

A configuration in which the queried model reaches the filtered property through a diamond of
references showed the effect plainly: eight chains for one filter, thirteen joins where two were
needed. It also broke the SQL outright. Table aliases are built from the last two classification
keys of the chain, so a two-model detour can produce the same alias as the queried model itself —
the alias appeared twice in one statement.

Breadth first fixes the cause rather than the symptom: every model is visited once, and the chain
recorded for it is the shortest, so the joins match the condition that is actually generated. It
also drops the walk from "all simple paths" to `O(V+E)`, which matters at compile time.

One consequence worth knowing before changing anything here: **a model reachable through two
genuinely different references is joined through one of them only.** That was already true —
`AddCodeSnippetFilterConditions` grouped the surplus chains away — but it is now a property of the
traversal instead of an accident. Per-path filters would need a different data structure, not a
tweak to this method.

### One snippet, many models

**A snippet with no `ModelName` is meant to apply globally** — to that property on every model that
has it, without an entry per model. Excluding one model, or one method, is what
`InfrastructureExceptionCodeSnippet` is for: it matches on `ClassName`, `MethodName` **and
`DataModelName`**, so the opt-out is per model.

The condition itself is emitted on three paths, and all three have to honour that granularity:

| Path | Where | Granularity |
|---|---|---|
| into the SQL text | `AddCodeSnippetReadConditions`, once per joined table alias | per joined model |
| into the LINQ filter, property on the queried model | `AddCodeSnippetFilterConditions`, `InfrastructureModel` overload | per queried model |
| into the LINQ filter, property reached through a chain | `AddCodeSnippetFilterConditions`, snippet-list overload | **grouped — see below** |

The third one groups the snippets before emitting, and that key **must carry the model the property
was found on**, not just the snippet. It used to be `CodeSnippeKey` plus property name only. For a
global snippet that key is identical for every model — so as soon as one query reached two models
carrying the property, the two collapsed into one group and only one condition survived. Silently,
and not the condition the exceptions were written against: `.First()` picks whatever the traversal
reached first. The joins for the dropped one were still generated, because the join step does not
group.

A filter that isolates data by owner is exactly the kind of snippet that gets applied globally, so a
dropped condition means a query returning rows it must not return — and no test notices as long as
the test data has one owner. The key therefore includes domain and model name.

Adding a property named like a global snippet's property to a new model is enough to reach this: the
snippet picks the model up automatically. That is the intended convenience, and it is why the key
has to keep the models apart.

### Only the third path depends on ForceAsWhereCondition

`ForceAsWhereCondition` is what puts a snippet on the third path at all — it is the filter
`QueryRepositoryTemplate` applies when collecting `whereConditionCodeSnippets`. A snippet without it
never produces an `ApplicableWhereConditionCodeSnippet`, so the traversal and the grouping above are
never reached; only the first two paths run. A consumer can therefore use the whole snippet
mechanism heavily and be completely unaffected by anything in this section — worth knowing before
concluding from one configuration that a change here is harmless.

The three paths also differ in **where the joins come from**, which decides whether reducing a
global snippet to a single model could ever pay off:

* On the first two paths the joins come from the DTO and reference structure. The snippet only
  attaches a condition to tables that are joined anyway, so dropping conditions saves nothing and
  removes filtering. On a snippet that isolates data by owner that is a straight regression.
* On the third path the snippet *creates* the joins. Only there would a reduction save anything.

So a "one match is enough" switch, should one ever be wanted, belongs on the third path alone.
Before adding it, weigh how little it buys: it changes nothing unless one query reaches two models
carrying the property, and the existing `DataModelName` exception already expresses the same intent
explicitly, per method. Real configurations lean on the per-model behaviour — one of them needs
three exceptions on a single method that differ in nothing but `DataModelName`, because one method
joins three models that all carry the property.

Both templates prune snippets that a method excludes through `InfrastructureExceptionCodeSnippet`,
but at different points, because they build their data at different times:

* `QueryRepositoryTemplate` rebuilds the related data models inside every method and filters the
  snippet list first — `FilterApplicableWhereConditionCodeSnippets`.
* `RepositoryTemplate` builds them once per domain model map, before any method is known, so it
  can only prune afterwards on the resulting items — `FilterQueryAnalysisItemsForApplicable
  WhereConditionCodeSnippets`, called in `GetReadByQuery`, the single place that template emits
  SQL.

That asymmetry is intentional. The two prune conditions are not quite identical though: the
item-level one additionally discards a snippet whose `UseInstead` exception carries no expression
on either side.

**When changing any of this, generate a full configuration before and after and compare the
output byte for byte** — normalise the tick-based `filterValueFor…` variable names first. The
factory classes can be run outside a generator, which makes that cheap. Joins may shrink; the
`Where.Add` expressions and the selected columns must not change.

## Dependencies

Consumes `Eshava.Core`, `Eshava.CodeAnalysis` and `Eshava.DomainDrivenDesign` as NuGet packages.
This repository sits at the bottom of the dependency graph and is the one most likely to need a
coordinated release.

**A change to the abstract base classes in `Eshava.DomainDrivenDesign` usually forces a change
to the templates here.** The layer rules of that repository — one-directional layer access,
self-validating models, immutable value objects, model events on save — are the contract the
generated code has to satisfy; see its `CLAUDE.md`.
