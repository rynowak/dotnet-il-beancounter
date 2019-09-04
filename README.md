# dotnet-il-beancounter

dotnet tool for analyzing size and characteristics of IL

## Goals and Scope

Produce useful tooling for understanding the composition of .NET assembly files in terms of their size. We need to optimize the size of the assemblies included with Blazor, and understanding what they are made of is the first step.

## Install

CI builds are pushed to `https://pkgs.dev.azure.com/nowakra/dotnet-il-beancounter/_packaging/public/nuget/v3/index.json`.

```txt
dotnet tool install dotnet-il-beancounter --version 0.1.0-* --global --add-source https://pkgs.dev.azure.com/nowakra/dotnet-il-beancounter/_packaging/public/nuget/v3/index.json
```

## PE Info

Use the `pe` command to show aggregate information about the PE format and .NET metadata.

```txt
dotnet il-beancounter pe -i ./test

Processing: /Users/ryan/test/dotnet-il-beancounter.dll
Total size is: 11264 bytes
PE Header is 512 bytes
Section .text is 8704 bytes
Section .rsrc is 1536 bytes
Section .reloc is 512 bytes

Analyzing .text section and COR header (used by .NET)
Metadata is 6116 bytes
Strong-Name Signature is 0 bytes
Resources is 0 bytes
IL is 1842 bytes
Unaccounted size: 458 bytes

Processing: /Users/ryan/test/System.CommandLine.dll
Total size is: 163920 bytes
PE Header is 512 bytes
Certificate Table is 9296 bytes
Section .text is 151552 bytes
Section .rsrc is 2048 bytes
Section .reloc is 512 bytes

Analyzing .text section and COR header (used by .NET)
Metadata is 99428 bytes
Strong-Name Signature is 128 bytes
Resources is 0 bytes
IL is 50664 bytes
Unaccounted size: 880 bytes
```

## IL

Use the `il` command to show the size of IL code (and EH table data) with aggregation at the assembly/namespace/type/method level.

```txt
> dotnet il-beancounter il -i ./test -g namespace

ILBeanCounter: 1842 bytes
System.CommandLine: 24695 bytes
System.CommandLine.Parsing: 5383 bytes
System.CommandLine.Invocation: 14061 bytes
System.CommandLine.Builder: 2126 bytes
System.CommandLine.Binding: 4399 bytes
```

Add `-f <prefix>` to filter by a string prefix, this is useful when grouping by type or method which are more verbose. Filters are a simple text prefix (`.StartsWith`) applied to the fully-qualified name, and are case-sensitive as all things should be.

```txt
dotnet il-beancounter il -i ./test -g method -f ILBeanCounter.Program

ILBeanCounter.Program.Main: 71 bytes
ILBeanCounter.Program.ExecuteAsync: 1241 bytes
ILBeanCounter.Program..ctor: 9 bytes
ILBeanCounter.Program.<Main>: 32 bytes
ILBeanCounter.Program+<>c..cctor: 12 bytes
ILBeanCounter.Program+<>c..ctor: 9 bytes
ILBeanCounter.Program+<>c.<Main>b__0_0: 25 bytes
ILBeanCounter.Program+<Main>d__0..ctor: 9 bytes
ILBeanCounter.Program+<Main>d__0.MoveNext: 432 bytes
ILBeanCounter.Program+<Main>d__0.SetStateMachine: 2 bytes
```
