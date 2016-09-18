The library uses the following versioning scheme for the assemblies and the NuGet packages.  
`<major>.<minor>.<patch>`

So for example, the version at the time this was written was:  
`1.2.0`

## Assembly Version

```
major  - increases when there are breaking changes that require app code updates.
minor  - increases when there are breaking changes that require only app code recompilication.
patch  - typically do not change.
```

Assemblies in the package are signed with a published key, so they are strongly named.
`major` and `minor` version numbers are increased as shown above. When a new release does not have
any breaking changes, assembly version remains the same. This enables the application to do
an in-place upgrade without recompiling or updating the app.

## Assembly File Version

Follows the NuGet package version.

## NuGet Package Version

Follows the same rules in Assembly Version to increase `major` and `minor` numbers. If `major` and `minor` are
not increased, `patch` is incremented in each release; otherwise, `patch` is reset to 0.
