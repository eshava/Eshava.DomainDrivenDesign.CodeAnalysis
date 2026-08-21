# Changelog

Notable changes per released version, newest first. Versions before 1.2.21 are not documented here —
the Git history is the source for those.

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
* Conflicts between namespaces and model names.
* The `EventDomainProperty` calculation.
* A method name conflict in api routes.
* Collecting the usings of domain models in the create, update and deactivate use cases.
