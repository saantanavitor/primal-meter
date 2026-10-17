// Uso no desenvolvimento: baixa e corrige o motor numa pasta, para compilar o NotACT e o Rhalgr Meter.
//   dotnet run --project src/EngineSetup.Cli -- external_dependencies
using RhalgrMeter.EngineSetup;

string folder = args.Length > 0 ? Path.GetFullPath(args[0]) : EngineInstaller.DefaultFolder;
var version = EngineInstaller.Ensure(folder, "dev", Console.WriteLine);
Console.WriteLine($"Motor do ACT {version} pronto em {folder}");
