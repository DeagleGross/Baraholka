IEnumerable<int> a = new List<int> { 1, 2, 3, 4, 5 };
var b = a.Where(x => x > 10);

foreach (var x in b)
{
    Console.WriteLine(x);
}
Console.WriteLine("hello");