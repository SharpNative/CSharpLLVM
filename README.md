# CSharpLLVM

CSharpLLVM is an LLVM-based compiler for C#. The main goal for this compiler is to use it in low-level system development (e.g. kernels), where you the .NET-framework is unavailable.

Currently the short-term todo is:
  - Array support
  - Structs
  - Class support (inheritance, casting)
  - Interface support
  - Command line arguments

Because this will be mainly used as a compiler for C# kernels, this means we will not support the following most notable features:
  - Garbage collection: implementing this would make the kernel slower, and it is more difficult to implement because certain objects may be coming from userspace
  - No .NET-framework

### Dependencies

We are using a NuGET package called Swigged.LLVM to be able to use LLVM in C#.
