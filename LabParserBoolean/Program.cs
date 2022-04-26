using LabParserBoolean;

string file = args.Length > 0 ? args[0] : "input.txt";
string fileContent = File.ReadAllText(file);

var tokens = new ScannerRegex(fileContent).Scan();
Console.WriteLine("Scanner output:");
Console.WriteLine(string.Join(" ", tokens.Select(t => $"{t.Kind}({t.Value})")));
Console.WriteLine();

var parser = new Parser(tokens);
var errorsList = parser.Parse();

Console.WriteLine("Parser output:");
Console.WriteLine($"Found {errorsList.Count()} errors.");
errorsList.ToList().ForEach(e =>
{
    Console.WriteLine("- {0}", e);
});
