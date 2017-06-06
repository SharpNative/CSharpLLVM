# CSharpLLVM

CSharpLLVM is an LLVM-based compiler for CIL (Common Intermediate Language). The main goal for this compiler is to use it in low-level system development (e.g. kernels) in the C# language, where the .NET-framework is unavailable.
While the project is currently still work-in-progress, we already support important features.

Because this will be mainly used as a compiler for C# kernels, this means we will not support the following most notable features:
  - Garbage collection: implementing this would make the kernel slower, and it is more difficult to implement because certain objects may be coming from userspace
  - No .NET-framework

### Runtime information

The compiler generates code that should be linked against some runtime methods that you'll need to implement.
There is a standard implementation available for these methods in runtime.c, but you can write custom implementations if you wish. More information about this (and a default implementation) can be found in the *runtime* folder.

### Dependencies

We are using a NuGET package called Swigged.LLVM to be able to use LLVM in C#.