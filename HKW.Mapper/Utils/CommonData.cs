using System;
using System.Collections.Generic;
using System.Text;

namespace HKW.SourceGeneratorUtils;

/// <summary>
/// 常用数据
/// </summary>
internal static class CommonData
{
    public const string TaskTypeFullName = "global::System.Threading.Tasks.Task";

    public static string GeneratedCodeAttribute { get; } =
        $"[global::System.CodeDom.Compiler.GeneratedCode(\"{System.Reflection.Assembly.GetCallingAssembly().GetName().Name}\",\"{System.Reflection.Assembly.GetCallingAssembly().GetName().Version}\")]";

    public const string DebuggerBrowsableNever =
        "[global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]";
}
