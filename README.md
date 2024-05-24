# Load Error

SDK 8.0.205 has `Microsoft.CodeAnalysis.CSharp.CodeStyle.dll` version `4.9`
and SDK 8.0.300 has `4.10`.

```bash
<dotnet-home>/<sdk-version>/Sdks/Microsoft.NET.Sdk/codestyle/cs/Microsoft.CodeAnalysis.CSharp.CodeStyle.dll
/usr/share/dotnet/sdk/8.0.300/
~/.dotnet/sdk/8.0.300/
```

`Microsoft.CodeAnalysis.CSharp.CodeStyle.dll` needs `Microsoft.CodeAnalysis.dll` when loading
(e.g. for [`SyntaxList<TNode>`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxlist-1?view=roslyn-dotnet-4.9.0)).
There is an incompatibility between `Microsoft.CodeAnalysis.CSharp.CodeStyle.dll` `4.10` and `Microsoft.CodeAnalysis.dll` `4.9`.
