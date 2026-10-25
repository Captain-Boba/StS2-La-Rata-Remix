using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

// Supply actual workspace English tables for native command/status messages.
// This is fixture initialization, not engine/PCK discovery or localization QA.
internal sealed class NativeFixtureLocalization : IDisposable
{
    private readonly LocManager oldManager=LocManager.Instance;
    private readonly object? oldFormatter=AccessTools.Field(typeof(LocManager),"_smartFormatter").GetValue(null);
    private readonly SmartFormat.SmartFormatter oldSmart=SmartFormat.Smart.Default;
    internal NativeFixtureLocalization()
    {
        var manager=(LocManager)RuntimeHelpers.GetUninitializedObject(typeof(LocManager));
        AccessTools.PropertySetter(typeof(LocManager),nameof(LocManager.Instance)).Invoke(null,[manager]);
        AccessTools.Method(typeof(LocManager),"LoadLocFormatters").Invoke(manager,null);
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../assets/LaRata/localization/eng"));
        var tables=Directory.GetFiles(root,"*.json").ToDictionary(p=>Path.GetFileNameWithoutExtension(p)!,p=>new LocTable(Path.GetFileNameWithoutExtension(p),JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(p))!));
        AccessTools.Field(typeof(LocManager),"_tables").SetValue(manager,tables);
        NativeConsoleFixture.Set(manager,nameof(LocManager.CultureInfo),CultureInfo.GetCultureInfo("en-US"));
        NativeConsoleFixture.Set(manager,nameof(LocManager.Language),"eng");
    }
    public void Dispose()
    {
        AccessTools.PropertySetter(typeof(LocManager),nameof(LocManager.Instance)).Invoke(null,[oldManager]);
        AccessTools.Field(typeof(LocManager),"_smartFormatter").SetValue(null,oldFormatter);
        SmartFormat.Smart.Default=oldSmart;
    }
}
