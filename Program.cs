using dnlib.DotNet;

string path = string.Join(' ', args);
while (string.IsNullOrWhiteSpace(path))
{
    Console.WriteLine("Write path of file");
    path = Console.ReadLine() ?? "";
}

if (ModuleDefMD.Load(path) is not { } module)
{
    Console.WriteLine("Assembly file not found");
    return;
}

module.IsILOnly = true;
module.VTableFixups = null;
module.Assembly.PublicKey = null;
module.Assembly.HasPublicKey = false;

Console.WriteLine($"Loaded {module.Name}");
Console.WriteLine("Resolving References");

module.Context = ModuleDef.CreateModuleContext();
if (module.Context.AssemblyResolver is AssemblyResolver resolver)
    resolver.AddToCache(module);

Console.WriteLine("Creating Publicized DLL");

foreach (TypeDef? type in module.Assembly.Modules.SelectMany(m => m.Types))
{
    if (type is { IsPublic: false })
        type.Attributes = type.IsNested ? TypeAttributes.NestedPublic : TypeAttributes.Public;

    foreach (MethodDef method in type.Methods.Where(x => x is { IsPublic: false }))
        method.Access = MethodAttributes.Public;

    foreach (FieldDef field in type.Fields.Where(x => x is { IsPublic: false }))
        field.Access = FieldAttributes.Public;

    foreach (TypeDef? nested in type.NestedTypes.Where(x => x is { IsPublic: false, IsNested: true }))
        nested.Attributes = (nested.Attributes & ~TypeAttributes.NestedPrivate) | TypeAttributes.NestedPublic;

    foreach (PropertyDef? property in type.Properties)
    {
        if (property.GetMethod is {IsPublic: false})
            property.GetMethod.Access = MethodAttributes.Public;

        if (property.SetMethod is {IsPublic: false})
            property.SetMethod.Access = MethodAttributes.Public;
    }

    foreach (EventDef? @event in type.Events)
    {
        foreach (FieldDef? field in @event.DeclaringType.Fields)
            if (field.Name == @event.Name)
                field.Access = FieldAttributes.Private;

        if (@event.AddMethod is {IsPublic: false})
            @event.AddMethod.Access = MethodAttributes.Public;

        if (@event.RemoveMethod is {IsPublic: false})
            @event.RemoveMethod.Access = MethodAttributes.Public;

        if (@event.InvokeMethod is {IsPublic: false})
            @event.InvokeMethod.Access = MethodAttributes.Public;
    }
}

module.Write("Assembly-CSharp_public.dll");
Console.WriteLine("Created Publicized DLL");