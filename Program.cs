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
        PublicizeMethod(method);

    foreach (FieldDefinition? field in type.Fields.Where(x => x is { IsPublic: false }))
        PublicizeField(field, !field.Name.EndsWith(">k__BackingField"));

    foreach (PropertyDefinition? property in type.Properties)
    {
        if (property.GetMethod is { IsPublic: false })
            PublicizeMethod(property.GetMethod);

        if (property.SetMethod is { IsPublic: false })
            PublicizeMethod(property.SetMethod);
    }

    foreach (EventDefinition? @event in type.Events)
    {
        foreach (FieldDefinition? duplicateField in @event.DeclaringType.Fields.Where(x => x.Name == @event.Name))
            PublicizeField(duplicateField, false);

        if (@event.AddMethod is { IsPublic: false })
            PublicizeMethod(@event.AddMethod);

        if (@event.RemoveMethod is { IsPublic: false })
            PublicizeMethod(@event.RemoveMethod);

        if (@event.InvokeMethod is { IsPublic: false })
            PublicizeMethod(@event.InvokeMethod);
    }
}

void PublicizeMethod(MethodDefinition cl)
{
    cl.IsPrivate = false;
    cl.IsAssembly = false;
    cl.IsFamily = false;
    cl.IsFamilyAndAssembly = false;
    cl.IsFamilyOrAssembly = false;
    cl.IsPublic = true;
}

void PublicizeField(FieldDefinition cl, bool publicize = true)
{
    cl.IsAssembly = false;
    cl.IsFamily = false;
    cl.IsFamilyAndAssembly = false;
    cl.IsFamilyOrAssembly = false;

    if (publicize)
        cl.Attributes = (cl.Attributes & ~FieldAttributes.Private) | FieldAttributes.Public;
    else
        cl.Attributes = (cl.Attributes & ~FieldAttributes.Public) | FieldAttributes.Private;
}