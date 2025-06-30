// Log GC collection counts for each generation
for (int gen = 0; gen <= GC.MaxGeneration; gen++)
{
    Console.WriteLine($"Gen {gen} collection count: {GC.CollectionCount(gen)}");
}

// Get GC memory info and log with explanations
// Get GC memory info and log with explanations
var memoryInfo = GC.GetGCMemoryInfo();
Console.WriteLine("\nGC Memory Info:");
Console.WriteLine($"HeapSizeBytes: {memoryInfo.HeapSizeBytes} (Total heap size in bytes)");
Console.WriteLine($"FragmentedBytes: {memoryInfo.FragmentedBytes} (Bytes of fragmented space in the heap)");
Console.WriteLine($"HighMemoryLoadThresholdBytes: {memoryInfo.HighMemoryLoadThresholdBytes} (Threshold for high memory load)");
Console.WriteLine($"MemoryLoadBytes: {memoryInfo.MemoryLoadBytes} (Current memory load in bytes)");
Console.WriteLine($"TotalAvailableMemoryBytes: {memoryInfo.TotalAvailableMemoryBytes} (Total available memory for the GC)");
Console.WriteLine($"Index: {memoryInfo.Index} (GC index for the most recent GC)");
Console.WriteLine($"Generation: {memoryInfo.Generation} (Generation of the most recent GC)");
Console.WriteLine($"Compacted: {memoryInfo.Compacted} (Whether the most recent GC was compacting)");
Console.WriteLine($"Concurrent: {memoryInfo.Concurrent} (Whether the most recent GC was concurrent)");
Console.WriteLine($"FinalizationPendingCount: {memoryInfo.FinalizationPendingCount} (Objects pending finalization)");
Console.WriteLine($"PauseTimePercentage: {memoryInfo.PauseTimePercentage} (GC pause time as a percentage)");
Console.WriteLine($"PromotedBytes: {memoryInfo.PromotedBytes} (Bytes promoted to an older generation)");
Console.WriteLine($"PinnedObjectsCount: {memoryInfo.PinnedObjectsCount} (Number of pinned objects)");
// Removed CommittedBytes and ReservedBytes as they are not available