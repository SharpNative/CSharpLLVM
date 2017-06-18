# CSharpLLVM

CSharpLLVM is an LLVM-based compiler for CIL (Common Intermediate Language). The main goal for this compiler is to use it in low-level system development (e.g. kernels) in the C# language, where the .NET-framework is unavailable.
Note that the project is still work-in-progress and does not support all features of the C# language.

### Important note

Because this compiler will be mainly used for C# kernels, this means we will not support the following most notable features:
  - Garbage collection: implementing this would make the kernel slower, and it is more difficult to implement because certain objects may be coming from userspace
  - No .NET-framework

An important difference to the .NET framework is that we treat "System.Char" as 8-bit instead of 16-bit. This decision was influenced by the fact that this compiler's main goal is to work together with kernels.

### Some important features

A list of important features we support (this list does not contain all features):
  - Classes, (including virtual calls and inheritance), interfaces
  - Structs (with fixed buffer support)
  - Most opcodes for CIL
  - The basic types

Some notable features we currently don't support:
  - Boxing & unboxing (need some sort of garbage collector, or we could work around this, need to investigate)
  - Generic types
  - Linq (depends on classes in the .NET framework)

### Future

Short term planning:
  - Add support for delegates
  - Add support for "Plug", this means that you will be able to provide your own implementation of certain .NET classes and/or methods.
  - ...

This list can change alot. It mostly depends on what is needed, which we will find out while building the kernel.

### Runtime information

The compiler generates code that should be linked against some runtime methods that you'll need to implement.
There is a standard implementation available for these methods in runtime.c, but you can write custom implementations if you wish. More information about this (and a default implementation) can be found in the *runtime* folder.

### Dependencies

We are using a NuGET package called Swigged.LLVM, the package is a wrapper for LLVM so we can use LLVM in C#. (https://www.nuget.org/packages/swigged.llvm/)
For command line parsing we are also using a NuGET package called CommandLineParser. (https://www.nuget.org/packages/CommandLineParser/)