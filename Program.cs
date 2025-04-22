using Mono.Cecil;

string path = string.Join(' ', args);
while (string.IsNullOrWhiteSpace(path))
{
    Console.WriteLine("Write path of file");
    path = Console.ReadLine() ?? "";
}

string outputFileName = Path.GetFileNameWithoutExtension(path) + "_public" + Path.GetExtension(path);
string outputDirectory = Path.GetDirectoryName(path) ?? Environment.CurrentDirectory;
string outputPath = Path.Combine(outputDirectory, outputFileName);

DefaultAssemblyResolver resolver = new();
resolver.AddSearchDirectory(outputDirectory);

ReaderParameters readerParameters = new()
{
    ReadWrite = true,
    AssemblyResolver = resolver,
    ReadingMode = ReadingMode.Immediate
};

ModuleDefinition module = ModuleDefinition.ReadModule(path, readerParameters);

if (module is null)
{
    Console.WriteLine("Assembly file not found");
    return;
}

Console.WriteLine($"Loaded {module.Name}");

module.Attributes |= ModuleAttributes.ILOnly;
module.Attributes &= ~ModuleAttributes.StrongNameSigned;

if (module.Assembly is { Name: not null })
{
    module.Assembly.Name.PublicKey = null;
    module.Assembly.Name.PublicKeyToken = null;
    module.Assembly.Name.HasPublicKey = false;
}

Console.WriteLine("Resolving References");

foreach (TypeDefinition? type in module.Types)
    PublicizeType(type);


Console.WriteLine("Creating Publicized DLL");
Console.WriteLine($"Saving to: {outputPath}");
module.Write(outputPath);
Console.WriteLine("Created Publicized DLL");
return;

void PublicizeType(TypeDefinition type)
{
    if (type is { IsPublic: false, IsNestedPublic: false })
    {
        if (type.IsNested)
        {
            type.IsNestedPrivate = false;
            type.IsNestedFamily = false;
            type.IsNestedAssembly = false;
            type.IsNestedFamilyAndAssembly = false;
            type.IsNestedFamilyOrAssembly = false;
            type.IsNestedPublic = true;
        }
        else
        {
            type.IsNotPublic = false;
            type.IsPublic = true;
        }
    }

    foreach (TypeDefinition? nestedType in type.NestedTypes)
        PublicizeType(nestedType);

    foreach (MethodDefinition? method in type.Methods.Where(x => x is { IsPublic: false }))
    {
        method.IsPrivate = false;
        method.IsAssembly = false;
        method.IsFamily = false;
        method.IsFamilyAndAssembly = false;
        method.IsFamilyOrAssembly = false;
        method.IsPublic = true;
    }

    foreach (FieldDefinition? field in type.Fields.Where(x => x is { IsPublic: false }))
    {
        field.IsPrivate = false;
        field.IsAssembly = false;
        field.IsFamily = false;
        field.IsFamilyAndAssembly = false;
        field.IsFamilyOrAssembly = false;
        field.IsPublic = true;
    }

    foreach (PropertyDefinition? property in type.Properties)
    {
        if (property.GetMethod is { IsPublic: false })
        {
            property.GetMethod.IsPrivate = false;
            property.GetMethod.IsAssembly = false;
            property.GetMethod.IsFamily = false;
            property.GetMethod.IsFamilyAndAssembly = false;
            property.GetMethod.IsFamilyOrAssembly = false;
            property.GetMethod.IsPublic = true;
        }

        if (property.SetMethod is { IsPublic: false })
        {
            property.SetMethod.IsPrivate = false;
            property.SetMethod.IsAssembly = false;
            property.SetMethod.IsFamily = false;
            property.SetMethod.IsFamilyAndAssembly = false;
            property.SetMethod.IsFamilyOrAssembly = false;
            property.SetMethod.IsPublic = true;
        }
    }

    foreach (EventDefinition? @event in type.Events)
    {
        foreach (FieldDefinition? field in @event.DeclaringType.Fields.Where(field => field.Name == @event.Name))
        {
            field.IsPrivate = true;
            field.IsAssembly = false;
            field.IsFamily = false;
            field.IsFamilyAndAssembly = false;
            field.IsFamilyOrAssembly = false;
            field.IsPublic = false;
        }

        if (@event.AddMethod is { IsPublic: false })
        {
            @event.AddMethod.IsPrivate = false;
            @event.AddMethod.IsAssembly = false;
            @event.AddMethod.IsFamily = false;
            @event.AddMethod.IsFamilyAndAssembly = false;
            @event.AddMethod.IsFamilyOrAssembly = false;
            @event.AddMethod.IsPublic = true;
        }

        if (@event.RemoveMethod is { IsPublic: false })
        {
            @event.RemoveMethod.IsPrivate = false;
            @event.RemoveMethod.IsAssembly = false;
            @event.RemoveMethod.IsFamily = false;
            @event.RemoveMethod.IsFamilyAndAssembly = false;
            @event.RemoveMethod.IsFamilyOrAssembly = false;
            @event.RemoveMethod.IsPublic = true;
        }

        if (@event.InvokeMethod is { IsPublic: false })
        {
            @event.InvokeMethod.IsPrivate = false;
            @event.InvokeMethod.IsAssembly = false;
            @event.InvokeMethod.IsFamily = false;
            @event.InvokeMethod.IsFamilyAndAssembly = false;
            @event.InvokeMethod.IsFamilyOrAssembly = false;
            @event.InvokeMethod.IsPublic = true;
        }
    }
}