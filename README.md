# Load Error

SDK 8.0.205 has `Microsoft.CodeAnalysis.CSharp.CodeStyle.dll` version `4.9`
and SDK 8.0.300 has `4.10`.

```text
<dotnet-home>/<sdk-version>/Sdks/Microsoft.NET.Sdk/codestyle/cs/Microsoft.CodeAnalysis.CSharp.CodeStyle.dll
/usr/share/dotnet/sdk/8.0.300/
~/.dotnet/sdk/8.0.300/
```

`Microsoft.CodeAnalysis.CSharp.CodeStyle.dll` needs `Microsoft.CodeAnalysis.dll` when loading
(e.g. for [`SyntaxList<TNode>`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxlist-1?view=roslyn-dotnet-4.9.0)).
There is an incompatibility between `Microsoft.CodeAnalysis.CSharp.CodeStyle.dll` `4.10` and `Microsoft.CodeAnalysis.dll` `4.9`.

## Reproduce

- Use `dotnet test` to run the test case.
- The NuGet reference
  [Microsoft.CodeAnalysis.CSharp](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/)
  provides `Microsoft.CodeAnalysis.dll` through the
  [Microsoft.CodeAnalysis.Common](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Common/)
  dependency.
- The package reference `<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.9.2"/>`
  will only pass the test with SDK 8.0.205.
- See [`csproj`](./LoadError.csproj) and [`global.json`](./global.json).
