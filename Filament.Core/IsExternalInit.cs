// Polyfill: `init` accessors and positional records require this type, which
// only ships in net5.0+. netstandard2.1 needs it defined locally.
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
