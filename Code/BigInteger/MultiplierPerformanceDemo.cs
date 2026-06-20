using System.Diagnostics;
using System.IO;

namespace Arithmetic;

public static class MultiplierPerformanceDemo
{
    public static void Run(TextWriter output, int wordCount = 2048)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (wordCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wordCount));
        }

        uint[] left = CreateDigits(wordCount, seed: 17);
        uint[] right = CreateDigits(wordCount, seed: 29);

        Measure(output, "Simple", new SimpleMultiplier(), left, right);
        Measure(output, "Karatsuba", new KaratsubaMultiplier(), left, right);
        Measure(output, "FFT", new FftMultiplier(), left, right);
    }

    private static void Measure(TextWriter output, string name, IMultiplier multiplier, uint[] left, uint[] right)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        uint[] result = multiplier.Multiply(left, right);
        stopwatch.Stop();

        output.WriteLine($"{name}: {stopwatch.ElapsedMilliseconds} ms, result words: {result.Length}");
    }

    private static uint[] CreateDigits(int count, int seed)
    {
        Random random = new(seed);
        uint[] digits = new uint[count];

        for (int i = 0; i < digits.Length; i++)
        {
            digits[i] = (uint)random.NextInt64(1, uint.MaxValue);
        }

        digits[^1] |= 1;
        return digits;
    }
}
