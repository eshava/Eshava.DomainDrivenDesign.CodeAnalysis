# Changelog

Notable changes per released version, newest first. Versions before 1.2.21 are not documented here —
the Git history is the source for those.

## 1.2.24

### Breaking

* **The lower bound for `Eshava.CodeAnalysis` moves to 1.0.8.** The endpoint fix below is generated
  with `CoalesceAssign`, which that version introduces. `Eshava.CodeAnalysis` flows on to the
  consumers, so a generator project that declares an older version of it alongside this package
  does not restore — `NU1605`. Raise both together.

### Fixed

* **A POST or PUT endpoint handed a null request to the use case.** As far as ASP.NET Core is
  concerned the body is optional — in a project without `<Nullable>enable</Nullable>` an empty one
  is accepted and arrives as null — and the generated method only wrapped its assignments in
  `if (request is not null)`. Everything the route contributed was dropped without a trace in that
  case: the values a code snippet supplies, the identifier out of the route. What reached the use
  case was null, and the first property access inside it threw. The generated catch block turned
  that into a logged error and a 500, for what is a client error.

  The guard is gone, replaced by `request ??= new <Request>();` ahead of the assignments. The route
  values are therefore always assignable, and a missing body is left to the validation inside the
  use case. **The nested guard for the dto property stays**: in a partial put that property is a
  `PartialPutDocument<T>`, where an empty instance would read as "a put without a single field"
  rather than as a missing body.

## 1.2.23

### Fixed

* **`CheckValidationConstraintsAsync` declared the same patch variable twice and did not compile.**
  A unique rule declares the patch of its own property for the whole method and the patch of each
  related property inside the `if` block that guards the check. Where a property is the subject of
  one unique rule and a related property of another, both declarations name the same variable, one
  enclosing the other — which C# rejects as `CS0136`, so the generated use case broke the build of
  the consuming project. The patches are declared up front now, one per property, before any of the
  checks. That is independent of the order the rules are written in: declaring them lazily would
  have moved the problem to `CS0841` when the related use comes first.

  Two side effects, both harmless. A related property that is not the subject of a rule moves its
  declaration out of the `if` block to the top of the method — `patches.FirstOrDefault(…)` has no
  side effect, so this is placement, not behaviour. And a property related to several rules is
  looked up once instead of once per rule.

### Known

* **A dto property two or more references away from the queried model loses its joins.** The
  generated `SELECT` names a table alias that no `JOIN` introduces, so the statement is rejected by
  the database while the generated C# compiles. Excluding that one query repository method from
  generation and writing it by hand is the way around it. The cause, why the obvious correction
  makes other queries worse, and what a real fix needs are in the "Known Defect: Missing Joins
  Beyond The Second Reference" section of `CLAUDE.md`.

## 1.2.22

### Breaking

* **Generated request classes carry the `JsonIgnore` attribute of one serializer, not of both.**
  Every request class hides its route and identifier properties from the serializer, and it did so
  with `Newtonsoft.Json.JsonIgnore` **and** `System.Text.Json.Serialization.JsonIgnore` on every one
  of them. That obliged a consuming project to reference both packages regardless of which one it
  serializes with — and where it compiled anyway, it compiled on a transitive dependency several
  levels down rather than on anything the project had asked for. The new `UseNewtonsoftJson` switch
  on the application project configuration decides which one is emitted. It defaults to `false`,
  which means `System.Text.Json`, so **a project serializing with Newtonsoft has to set it** — the
  attribute it needs is no longer emitted by default. A project on `System.Text.Json` needs no
  change and loses a package it never used.

  One consequence of picking one instead of both: with the switch on, the openapi schema generation
  of ASP.NET Core no longer sees that those properties are ignored, because it reads
  `System.Text.Json` metadata whichever serializer serves the requests. The route and identifier
  properties then appear in the request schema of the document.

### Fixed

* **A dto property the serializer ignores took over the attributes of its domain model property.**
  That takeover is what puts `Required`, `Range` and the rest onto a dto without repeating them per
  use case, and it ran for every property. On a property carrying `JsonIgnore` it produced a
  contradiction — required, but never deserialized — and `System.Text.Json` refuses such a type
  outright when a schema is exported from it. The consequence was not a wrong schema for that one
  property: the openapi document of the whole service failed to build, because generation stops at
  the first type it cannot describe, and every endpoint taking such a dto in its request body pulls
  the type in. A property the serializer ignores now takes over nothing, while the attributes
  configured on the dto itself stay untouched. Only the type name of the attribute is compared, so a
  qualified name, a bare one, and either of them with the `Attribute` suffix are all recognised — the
  library writes the name qualified itself, a configuration usually does not.

### Changed

* **The attributes collected for a generated property are no longer written back into the
  configuration.** `CollectPropertyUsings` appended what the domain model contributed to the list on
  the dto property it had been handed. Several templates walk the same configuration objects, so
  what a property ended up with depended on which of them had run before. It works on a copy now.
  The generated output does not change: everything the example generator produces was generated
  before and after and compared, 1673 sources across the four layers, byte for byte identical once
  the tick-based parameter names are normalised.
* The code snippet of the example generator declared its request property with both `JsonIgnore`
  attributes as well. It declares the `System.Text.Json` one only, matching the new default — a
  snippet is consumer configuration, so the switch does not reach it and the example has to show
  what it means.

## 1.2.21

### Breaking

* **The `ExecuteBefore` hooks changed with `Eshava.DomainDrivenDesign`.** The read use case now
  generates an `ExecuteBeforeAsync` call where it generated none, and the call in the update use
  case passes the id of the domain model in addition to the patches and the model. A hand written
  implementation has to follow, and the consuming project has to be on the version of
  `Eshava.DomainDrivenDesign` these hooks belong to.
* **`Eshava.CodeAnalysis` 1.0.7 is required.** A consuming generator project that also references
  that package directly has to raise it as well — NuGet reports a lower direct reference as
  `NU1605`, which is a restore error, not a warning. No code change is needed for it: the members
  renamed in 1.0.7 are used inside this library only.

### Added

* An option on the `FromDomainModel` method to change how property values are mapped.
* A domain model option that prevents the add method on an aggregate.
* Where condition behaviour for infrastructure code snippets, later extended to configurable code
  conditions.
* Code snippets for mappings in query repositories, including the exceptions to them.

### Changed

* In the deactivate use case the `ExecuteBefore` and `ExecuteBeforeAutoGen` hooks run earlier in the
  method.
* Joined tables were emitted as `LEFT JOIN` throughout. A table for which an applied code snippet
  contributes a read condition is emitted as an inner `JOIN` now — the condition reduces the outer
  join to an inner one anyway, so the statement says what it does. Without such a condition it stays
  a `LEFT JOIN`, which is also still the default of `GetJoinsQueryParts`.
* **Updated to `Eshava.CodeAnalysis` 1.0.7.** That release renamed a number of members, and the call
  sites here follow — see its own changelog for the list. The renames are the whole migration: the
  generated code is unchanged, verified by generating everything the example generator produces with
  1.0.6 and with 1.0.7 and comparing, 595 sources, byte for byte identical.

### Fixed

* **Query parameters of an api endpoint were generated as required parameters in configuration
  order.** They are optional now — emitted with `= default` — and therefore have to come last,
  which is where they are placed. The parameter construction moved into
  `ProcessApiRouteParameters`.
* **The conditions for additional parameters ended up outside the `WHERE` clause.** In
  `CreateIsUniqueMethod` they were appended to the `FROM` part — before the joins, and before the
  `WHERE` keyword had been written at all. The result was an `AND` with nothing to attach to, so the
  generated statement was invalid as soon as a method had additional parameters. They belong to the
  `WHERE` clause and are emitted there now, after the soft delete status condition.
* **Surplus SQL joins and broken statements for where condition code snippets.** Collecting the
  applicable snippets walked the model references depth first and enumerated *all simple paths* to
  the filtered property, so one filter produced one applicable snippet per path and one join per
  edge of every path — while the condition itself was emitted once. Joins and conditions were
  computed from different populations. A configuration reaching the property through a diamond of
  references produced thirteen joins where two were needed, and duplicate table aliases within one
  statement. The walk is breadth first now: every model is visited once, the recorded chain is the
  shortest, and the joins match the condition that is generated. See the "Where Condition Code
  Snippets" section of `CLAUDE.md` before changing anything there.

  Two parts of the same correction: table aliases beyond the first join level are derived from two
  elements of the model chain instead of the model name alone, so the same table reached through
  different chains no longer collides with the alias of the queried model. And a model that is part
  of a query only because a code snippet reaches through it now carries `IsCodeSnippetRelated`, so
  the repository template treats it like a model that exists for the join calculation alone.
* **One filter condition per model, for a code snippet that applies to more than one.** A snippet
  without a `ModelName` matches its property on every model that has it — that is what makes it
  global — but the conditions were grouped by the snippet and the property name alone. The model the
  property had been found on was nowhere in that key, so as soon as one query reached two models
  carrying the property, both fell into one group and one of the two conditions was dropped while
  the joins for it were still generated. Which one survived was whatever the traversal reached
  first, so an exception written against a particular model could miss its target. The key carries
  the domain and the model name now, which is the granularity `InfrastructureExceptionCodeSnippet`
  already works on. No known configuration reaches two such models today; the generated output of
  the example generator is unchanged.
* Conflicts between namespaces and model names.
* The `EventDomainProperty` calculation.
* A method name conflict in api routes.
* Collecting the usings of domain models in the create, update and deactivate use cases.
